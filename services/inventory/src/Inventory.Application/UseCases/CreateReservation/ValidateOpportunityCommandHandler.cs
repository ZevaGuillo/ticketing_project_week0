using Inventory.Application.UseCases.CreateReservation;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.UseCases.CreateReservation;

public class ValidateOpportunityCommandHandler : IRequestHandler<ValidateOpportunityCommand, ValidateOpportunityResult>
{
    private readonly InventoryDbContext _context;
    private readonly IOpportunityWindowRepository _opportunityWindowRepository;
    private readonly IReservationRepository _reservationRepository;
    private const int ReservationTtlMinutes = 15;

    public ValidateOpportunityCommandHandler(
        InventoryDbContext context,
        IOpportunityWindowRepository opportunityWindowRepository,
        IReservationRepository reservationRepository)
    {
        _context = context;
        _opportunityWindowRepository = opportunityWindowRepository;
        _reservationRepository = reservationRepository;
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

        opportunity.Status = OpportunityStatus.IN_PROGRESS;
        await _opportunityWindowRepository.UpdateAsync(opportunity, cancellationToken);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            SeatId = opportunity.SeatId,
            CustomerId = waitlistEntry.UserId.ToString(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ReservationTtlMinutes),
            Status = "active"
        };

        await _reservationRepository.AddAsync(reservation, cancellationToken);

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
