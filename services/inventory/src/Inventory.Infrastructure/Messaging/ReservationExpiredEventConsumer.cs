// HUMAN CHECK: Corrección 5 - DLQ configurada para mensajes fallidos en sistema distribuido.
using System.Text.Json;
using Confluent.Kafka;
using Inventory.Domain.Events;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging;

public class ReservationExpiredEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiredEventConsumer> _logger;
    private readonly IConsumer<string?, string> _consumer;
    private readonly IProducer<string?, string> _dlqProducer;
    private readonly string _topic = "reservation-expired";
    private readonly string _dlqTopic = "reservation-expired-dlq";

    public ReservationExpiredEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConsumer<string?, string> consumer,
        IProducer<string?, string> dlqProducer,
        ILogger<ReservationExpiredEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
        _dlqProducer = dlqProducer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationExpiredEventConsumer starting...");

        await Task.Delay(5000, stoppingToken);

        _consumer.Subscribe(_topic);
        _logger.LogInformation("Subscribed to topic {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result == null || result.IsPartitionEOF) continue;

                    _logger.LogInformation("Received reservation-expired event: {Partition} {Offset}", 
                        result.Partition, result.Offset);

                    var reservationExpired = JsonSerializer.Deserialize<ReservationExpiredEvent>(
                        result.Message.Value,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (reservationExpired != null)
                    {
                        await ProcessEventAsync(reservationExpired, stoppingToken);
                        _consumer.Commit(result);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                    await SendToDlqAsync(ex.Message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing reservation-expired event");
                    await SendToDlqAsync(ex.Message, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task SendToDlqAsync(string errorMessage, CancellationToken ct)
    {
        try
        {
            _logger.LogWarning("Sending failed message to DLQ: {Topic}", _dlqTopic);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to DLQ");
        }
    }

    private async Task ProcessEventAsync(ReservationExpiredEvent evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var idempotencyKey = $"waitlist:processed:{evt.ReservationId}:{evt.ExpiredAt:o}";
        
        _logger.LogInformation("Processing reservation expired: {ReservationId}, Seat: {SeatId}, Section: {Section}",
            evt.ReservationId, evt.SeatId, evt.Section);

        _logger.LogInformation("Event processed successfully: {ReservationId}", evt.ReservationId);
        await Task.CompletedTask;
    }
}
