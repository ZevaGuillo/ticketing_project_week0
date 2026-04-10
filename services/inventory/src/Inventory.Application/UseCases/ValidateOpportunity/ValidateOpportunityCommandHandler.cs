using System.Text.Json;
using Inventory.Application.DTOs;
using Inventory.Application.UseCases.CreateReservation;
using Inventory.Application.UseCases.ValidateOpportunity;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.UseCases.ValidateOpportunity;

public class ValidateOpportunityCommandHandler : IRequestHandler<ValidateOpportunityCommand, ValidateOpportunityResult>
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly InventoryDbContext _context;
    private readonly IOpportunityWindowRepository _opportunityWindowRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ReservationSettings _reservationSettings;

    public ValidateOpportunityCommandHandler(
        InventoryDbContext context,
        IOpportunityWindowRepository opportunityWindowRepository,
        IReservationRepository reservationRepository,
        IKafkaProducer kafkaProducer,
        ReservationSettings reservationSettings)
    {
        _context = context;
        _opportunityWindowRepository = opportunityWindowRepository;
        _reservationRepository = reservationRepository;
        _kafkaProducer = kafkaProducer;
        _reservationSettings = reservationSettings;
    }

    public async Task<ValidateOpportunityResult> Handle(ValidateOpportunityCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await _opportunityWindowRepository.GetByTokenAsync(request.Token, cancellationToken);
        
        if (opportunity == null)
        {
            throw new KeyNotFoundException($"Opportunity with token {request.Token} not found");
        }

        if (opportunity.Status == OpportunityStatus.USED)
        {
            throw new InvalidOperationException("This opportunity has already been used");
        }

        if (opportunity.Status == OpportunityStatus.EXPIRED || opportunity.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("This opportunity has expired");
        }

        var waitlistEntry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == opportunity.WaitlistEntryId, cancellationToken);
        
        if (waitlistEntry == null)
        {
            throw new InvalidOperationException("Waitlist entry not found");
        }

        var seat = await _context.Seats.FindAsync(new object[] { opportunity.SeatId }, cancellationToken);
        if (seat == null)
        {
            throw new InvalidOperationException("Seat not found");
        }

        opportunity.Status = OpportunityStatus.IN_PROGRESS;
        await _opportunityWindowRepository.UpdateAsync(opportunity, cancellationToken);

        var now = DateTime.UtcNow;
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            EventId = waitlistEntry.EventId,
            SeatId = opportunity.SeatId,
            CustomerId = waitlistEntry.UserId.ToString(),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_reservationSettings.TTLMinutes),
            Status = "active"
        };

        await _reservationRepository.AddAsync(reservation, cancellationToken);

        // Publish reservation-created so the Ordering service's in-memory store
        // learns about this reservation and allows adding it to the cart.
        var @event = new ReservationCreatedEvent(
            EventId: Guid.NewGuid().ToString("D"),
            ReservationId: reservation.Id.ToString("D"),
            CustomerId: reservation.CustomerId,
            SeatId: reservation.SeatId.ToString("D"),
            SeatNumber: $"{seat.Section}-{seat.Row}-{seat.Number}",
            Section: seat.Section,
            BasePrice: 0m,
            CreatedAt: reservation.CreatedAt.ToString("O"),
            ExpiresAt: reservation.ExpiresAt.ToString("O"),
            Status: reservation.Status
        );
        var json = JsonSerializer.Serialize(@event, CamelCaseOptions);
        await _kafkaProducer.ProduceAsync("reservation-created", json, reservation.SeatId.ToString("N"));

        return new ValidateOpportunityResult
        {
            OpportunityId = opportunity.Id,
            Token = request.Token,
            UserId = waitlistEntry.UserId,
            EventId = waitlistEntry.EventId,
            SeatId = opportunity.SeatId,
            Section = waitlistEntry.Section,
            ExpiresAt = reservation.ExpiresAt,
            ReservationId = reservation.Id
        };
    }
}
