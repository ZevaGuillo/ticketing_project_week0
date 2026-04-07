using System.Text.Json;
using Catalog.Application.Ports;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging;

public class CatalogEventConsumer : BackgroundService
{
    private const string SeatIdProperty = "seatId";
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CatalogEventConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string[] _topics = { "reservation-created", "reservation-expired", "payment-succeeded", "seat-released", "waitlist-opportunity" };
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

            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 30000
            };
            
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(_topics);
            
            _logger.LogInformation("CatalogEventConsumer: Waiting for initial assignment...");
            var assignmentTimeout = DateTime.UtcNow.AddSeconds(30);
            while (!stoppingToken.IsCancellationRequested && DateTime.UtcNow < assignmentTimeout)
            {
                try
                {
                    var msg = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (msg != null && !msg.IsPartitionEOF)
                    {
                        consumer.Commit(msg);
                        break;
                    }
                }
                catch (ConsumeException) { }
            }

            _logger.LogInformation("CatalogEventConsumer started and subscribed to topics: {Topics}", string.Join(", ", _topics));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(10));
                    
                    if (result == null || result.IsPartitionEOF)
                        continue;
                    
                    await ProcessEventAsync(result.Topic, result.Message.Value);
                    consumer.Commit(result);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
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

    private async Task ProcessEventAsync(string topic, string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        try
        {
            var root = JsonDocument.Parse(message).RootElement;
            
            if (topic == "waitlist-opportunity")
            {
                JsonElement? seatIdElement = null;
                
                if (root.TryGetProperty("SeatId", out var pascalSeatId))
                    seatIdElement = pascalSeatId;
                else if (root.TryGetProperty(SeatIdProperty, out var camelSeatId))
                    seatIdElement = camelSeatId;
                
                if (seatIdElement.HasValue && Guid.TryParse(seatIdElement.Value.GetString(), out var seatId))
                {
                    var status = (root.TryGetProperty("Status", out var statusProp) || root.TryGetProperty("status", out statusProp))
                        ? statusProp.GetString() 
                        : "OFFERED";

                    if (status == "OFFERED")
                    {
                        await repository.UpdateSeatStatusAsync(seatId, "reserved", null);
                        _logger.LogInformation("Seat {SeatId} marked as reserved (opportunity offered)", seatId);
                    }
                    else if (status == "EXPIRED" || status == "USED")
                    {
                        await repository.UpdateSeatStatusAsync(seatId, "available", null);
                        _logger.LogInformation("Seat {SeatId} marked as available (opportunity {Status})", seatId, status);
                    }
                }
            }
            else if (topic == "reservation-created")
            {
                if (root.TryGetProperty(SeatIdProperty, out var seatIdProp) && Guid.TryParse(seatIdProp.GetString(), out var seatId))
                {
                    Guid? reservationId = null;
                    if (root.TryGetProperty("reservationId", out var resIdProp) && Guid.TryParse(resIdProp.GetString(), out var resId))
                        reservationId = resId;
                    
                    await repository.UpdateSeatStatusAsync(seatId, "reserved", reservationId);
                    _logger.LogInformation("Seat {SeatId} marked as reserved", seatId);
                }
            }
            else if (topic == "reservation-expired")
            {
                if (root.TryGetProperty(SeatIdProperty, out var seatIdProp) && Guid.TryParse(seatIdProp.GetString(), out var seatId))
                    await repository.UpdateSeatStatusAsync(seatId, "available");
                else if (root.TryGetProperty("reservationId", out var resIdProp) && Guid.TryParse(resIdProp.GetString(), out var resId))
                    await repository.UpdateSeatStatusByReservationAsync(resId, "available");
            }
            else if (topic == "payment-succeeded")
            {
                if (root.TryGetProperty("reservationId", out var resIdProp) && Guid.TryParse(resIdProp.GetString(), out var resId))
                    await repository.UpdateSeatStatusByReservationAsync(resId, "sold");
            }
            else if (topic == "seat-released"
                && root.TryGetProperty(SeatIdProperty, out var seatIdProp2) && Guid.TryParse(seatIdProp2.GetString(), out var seatId2))
            {
                await repository.UpdateSeatStatusAsync(seatId2, "available", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {Topic}", topic);
        }
    }
}