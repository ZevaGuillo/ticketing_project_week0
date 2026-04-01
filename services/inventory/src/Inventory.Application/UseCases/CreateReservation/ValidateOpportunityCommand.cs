using MediatR;

namespace Inventory.Application.UseCases.CreateReservation;

public record ValidateOpportunityCommand(string Token) : IRequest<ValidateOpportunityResult>;

public class ValidateOpportunityResult
{
    public Guid OpportunityId { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public Guid SeatId { get; set; }
    public string Section { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid ReservationId { get; set; }
}
