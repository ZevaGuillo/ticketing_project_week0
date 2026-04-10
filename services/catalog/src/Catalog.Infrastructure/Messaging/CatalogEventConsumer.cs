using System.Text.Json;
using Catalog.Application.Ports;
using Catalog.Infrastructure.Messaging.Strategies;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging;

public class CatalogEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CatalogEventConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string _groupId = "catalog-service-group";

    public CatalogEventConsumer(IServiceProvider serviceProvider, ILogger<CatalogEventConsumer> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? configuration["Kafka__BootstrapServers"] ?? "kafka:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("CatalogEventConsumer: Waiting for Kafka to be ready...");
            await Task.Delay(5000, stoppingToken);

            using var scope = _serviceProvider.CreateScope();
            var strategies = scope.ServiceProvider
                .GetRequiredService<IEnumerable<IKafkaEventStrategy>>()
                .ToDictionary(s => s.Topic);

            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 30000
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(strategies.Keys);

            _logger.LogInformation("CatalogEventConsumer started and subscribed to topics: {Topics}", string.Join(", ", strategies.Keys));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result == null || result.IsPartitionEOF) continue;

                    await DispatchAsync(result.Topic, result.Message.Value, strategies);
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CatalogEventConsumer: Shutdown initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CatalogEventConsumer fatal error");
        }
    }

    private async Task DispatchAsync(string topic, string message, Dictionary<string, IKafkaEventStrategy> strategies)
    {
        if (!strategies.TryGetValue(topic, out var strategy))
        {
            _logger.LogWarning("No strategy registered for topic '{Topic}'", topic);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        try
        {
            var root = JsonDocument.Parse(message).RootElement;
            await strategy.HandleAsync(root, repository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {Topic}", topic);
        }
    }
}