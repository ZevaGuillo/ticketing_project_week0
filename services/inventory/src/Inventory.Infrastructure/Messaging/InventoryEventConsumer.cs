using System.Text.Json;
using Confluent.Kafka;
using Inventory.Infrastructure.Messaging.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging;

public class InventoryEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryEventConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string _groupId = "inventory-service-group";

    public InventoryEventConsumer(
        IServiceProvider serviceProvider,
        ILogger<InventoryEventConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _bootstrapServers = configuration.GetSection("Kafka")["BootstrapServers"] ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Resolve strategy map from DI
        using var initialScope = _serviceProvider.CreateScope();
        var strategies = initialScope.ServiceProvider
            .GetRequiredService<IEnumerable<IInventoryEventStrategy>>()
            .ToDictionary(s => s.Topic);

        var topics = strategies.Keys.ToList();

        _logger.LogInformation("InventoryEventConsumer started and subscribed to topics: {Topics}", string.Join(", ", topics));

        await Task.Delay(5000, stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            EnablePartitionEof = true
        };

        using var consumer = new ConsumerBuilder<string?, string>(config).Build();
        consumer.Subscribe(topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result == null || result.IsPartitionEOF) continue;

                    _logger.LogInformation("Received event: {Topic} {Partition} {Offset}", result.Topic, result.Partition, result.Offset);

                    if (!strategies.TryGetValue(result.Topic, out var strategy))
                    {
                        _logger.LogWarning("No strategy found for topic {Topic}", result.Topic);
                        consumer.Commit(result);
                        continue;
                    }

                    var root = JsonDocument.Parse(result.Message.Value).RootElement;

                    using var scope = _serviceProvider.CreateScope();
                    await strategy.HandleAsync(root, scope, stoppingToken);

                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Inventory event on topic {Topic}", "unknown");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
