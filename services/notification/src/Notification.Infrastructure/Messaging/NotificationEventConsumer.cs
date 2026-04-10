using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Infrastructure.Messaging.Strategies;

namespace Notification.Infrastructure.Messaging;

public class NotificationEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationEventConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string _groupId = "notification-service-group";

    public NotificationEventConsumer(IServiceProvider serviceProvider, ILogger<NotificationEventConsumer> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? configuration["Kafka__BootstrapServers"] ?? "kafka:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var initialScope = _serviceProvider.CreateScope();
        var strategies = initialScope.ServiceProvider
            .GetRequiredService<IEnumerable<INotificationEventStrategy>>()
            .ToDictionary(s => s.Topic);

        var topics = strategies.Keys.ToList();

        _logger.LogInformation("NotificationEventConsumer: Waiting for Kafka to be ready...");
        await Task.Delay(5000, stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topics);

        _logger.LogInformation("NotificationEventConsumer started and subscribed to topics: {Topics}", string.Join(", ", topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result == null || result.IsPartitionEOF) continue;

                    if (!strategies.TryGetValue(result.Topic, out var strategy))
                    {
                        _logger.LogWarning("No strategy registered for topic '{Topic}'", result.Topic);
                        consumer.Commit(result);
                        continue;
                    }

                    var root = JsonDocument.Parse(result.Message.Value).RootElement;
                    using var scope = _serviceProvider.CreateScope();
                    await strategy.HandleAsync(root, scope, stoppingToken);

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
            _logger.LogInformation("NotificationEventConsumer: Shutdown initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationEventConsumer fatal error");
        }
        finally
        {
            consumer.Close();
        }
    }
}
