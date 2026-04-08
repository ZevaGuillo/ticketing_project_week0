using System.Text.Json;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Workers;

/// <summary>
/// Background worker that polls for expired reservations and publishes `reservation-expired` events.
/// Exposed `ProcessExpiredReservationsAsync` to allow unit tests to run the logic once.
/// </summary>
public class ReservationExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaProducer _producer;
    private readonly TimeSpan _pollInterval;

    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, IKafkaProducer producer)
        : this(scopeFactory, producer, TimeSpan.FromMinutes(1)) { }

    // constructor with configurable poll interval (used by tests)
    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, IKafkaProducer producer, TimeSpan pollInterval)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _pollInterval = pollInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReservationExpiryWorker error: {ex.Message}");
            }

            await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    public async Task ProcessExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var now = DateTime.UtcNow;
        var expirables = await db.Reservations
            .Where(r => r.Status == "active" && r.ExpiresAt <= now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!expirables.Any()) return;

        foreach (var res in expirables)
        {
            res.Status = "expired";

            var seat = await db.Seats.FindAsync(new object[] { res.SeatId }, cancellationToken).ConfigureAwait(false);
            if (seat != null)
            {
                seat.Reserved = false;
                db.Seats.Update(seat);
            }

            db.Reservations.Update(res);

            // Save changes BEFORE publishing events to prevent race condition
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // publish event with complete information for waitlist processing
            var @event = new
            {
                EventId = res.EventId,
                ReservationId = res.Id.ToString("D"),
                SeatId = res.SeatId.ToString("D"),
                Section = seat?.Section,
                ExpiredAt = res.ExpiresAt
            };

            var json = JsonSerializer.Serialize(@event);
            try
            {
                await _producer.ProduceAsync("reservation-expired", json, res.SeatId.ToString("N")).ConfigureAwait(false);
                Console.WriteLine($"Published reservation-expired event for reservation {res.Id}, seat {res.SeatId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to publish reservation-expired for {res.Id}: {ex.Message}");
            }

            // Publish seat-released event to notify Catalog service to update seat status
            var seatReleasedEvent = new
            {
                seatId = res.SeatId.ToString("D"),
                eventId = res.EventId.ToString("D"),
                status = "available"
            };

            var seatReleasedJson = JsonSerializer.Serialize(seatReleasedEvent);
            try
            {
                await _producer.ProduceAsync("seat-released", seatReleasedJson, res.SeatId.ToString("N")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to publish seat-released for {res.SeatId}: {ex.Message}");
            }
        }
    }
}
