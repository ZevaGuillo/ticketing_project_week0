using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Infrastructure.Messaging.Strategies;
using Payment.Infrastructure.Services;
using System.Text.Json;

namespace Payment.Infrastructure.Messaging;

/// <summary>
/// Background service that consumes payment-related events from Kafka.
/// Dispatches each message to the registered <see cref="IPaymentEventStrategy"/> for its topic.
/// </summary>
public class ReservationEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<ReservationEventConsumer> _logger;

    public ReservationEventConsumer(
        IServiceProvider serviceProvider,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<ReservationEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_kafkaOptions.EnableConsumer)
        {
            _logger.LogInformation("Kafka consumer is disabled, skipping reservation event consumption");
            return;
        }

        _logger.LogInformation("Starting reservation event consumer for payment service");

        await Task.Yield();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var strategies = scope.ServiceProvider
                .GetRequiredService<IEnumerable<IPaymentEventStrategy>>()
                .ToDictionary(s => s.Topic);

            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaOptions.BootstrapServers,
                GroupId = _kafkaOptions.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                EnableAutoOffsetStore = false
            };

            using var consumer = new ConsumerBuilder<string?, string>(config).Build();
            consumer.Subscribe(strategies.Keys);

            _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", strategies.Keys));

            var reservationStore = scope.ServiceProvider.GetRequiredService<ReservationStateStore>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    if (consumeResult?.Message?.Value != null)
                    {
                        await DispatchAsync(consumeResult.Topic, consumeResult.Message.Value, strategies, stoppingToken);
                        consumer.StoreOffset(consumeResult);
                    }

                    // Periodically cleanup expired reservations
                    reservationStore.CleanupExpiredReservations();
                }
                catch (OperationCanceledException) { break; }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming event: {Error}", ex.Error.Reason);
                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in event consumer");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            consumer.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in reservation event consumer");
        }
    }

    private async Task DispatchAsync(
        string topic,
        string message,
        Dictionary<string, IPaymentEventStrategy> strategies,
        CancellationToken cancellationToken)
    {
        if (!strategies.TryGetValue(topic, out var strategy))
        {
            _logger.LogWarning("No strategy registered for topic '{Topic}'", topic);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        try
        {
            var root = JsonDocument.Parse(message).RootElement;
            await strategy.HandleAsync(root, scope, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event for topic '{Topic}'", topic);
        }
    }
}