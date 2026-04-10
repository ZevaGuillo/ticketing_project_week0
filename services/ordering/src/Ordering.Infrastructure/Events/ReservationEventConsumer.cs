using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Infrastructure.Events.Strategies;

namespace Ordering.Infrastructure.Events;

/// <summary>
/// Background service that consumes reservation events from Kafka.
/// Dispatches each message to the registered <see cref="IOrderingEventStrategy"/> for its topic.
/// </summary>
public class ReservationEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationEventConsumer> _logger;
    private readonly KafkaOptions _kafkaOptions;

    public ReservationEventConsumer(
        IServiceProvider serviceProvider,
        ILogger<ReservationEventConsumer> logger,
        IOptions<KafkaOptions> kafkaOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _kafkaOptions = kafkaOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_kafkaOptions.EnableConsumer)
        {
            _logger.LogInformation("Kafka consumer is disabled");
            return;
        }

        // Wait a bit for Kafka to be ready in Docker environments
        await Task.Delay(2000, stoppingToken);

        int retryCount = 0;
        const int maxRetries = 5;
        const int retryDelayMs = 3000;

        while (!stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                await ConsumeMessagesAsync(stoppingToken);
                retryCount = 0; // Reset on successful connection
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Kafka consumer error (attempt {RetryCount}/{MaxRetries}). Retrying in {DelayMs}ms...",
                    retryCount, maxRetries, retryDelayMs);

                if (retryCount < maxRetries)
                    await Task.Delay(retryDelayMs, stoppingToken);
            }
        }

        if (retryCount >= maxRetries)
            _logger.LogError("Kafka consumer failed after {MaxRetries} attempts. Shutting down.", maxRetries);
    }

    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var strategies = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IOrderingEventStrategy>>()
            .ToDictionary(s => s.Topic);

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaOptions.ConsumerGroupId,
            // Use Earliest in smoke-tests / MVP so consumer reads messages
            // that may have been published before the service subscribed.
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            ConnectionsMaxIdleMs = 30000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .Build();

        try
        {
            consumer.Subscribe(strategies.Keys);
            _logger.LogInformation("Started consuming events from Kafka topics: {Topics}", string.Join(", ", strategies.Keys));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // HUMAN CHECK: Consumo de eventos con reintento manual.
                    // Se identificó deuda técnica: se recomienda migrar a Polly para manejar
                    // reintentos con Exponential Backoff y Circuit Breaker.
                    var consumeResult = consumer.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value != null)
                        await DispatchAsync(consumeResult.Topic, consumeResult.Message.Value, strategies, stoppingToken);
                }
                catch (ConsumeException ex) { _logger.LogError(ex, "Error consuming Kafka message"); }
                catch (JsonException ex) { _logger.LogError(ex, "Error deserializing Kafka message"); }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Unexpected error processing Kafka message"); }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Kafka consumer");
            throw;
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka consumer closed");
        }
    }

    private async Task DispatchAsync(
        string topic,
        string message,
        Dictionary<string, IOrderingEventStrategy> strategies,
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