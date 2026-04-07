using MediatR;

namespace Inventory.Application.UseCases.ProcessWaitlistSelection;

public record ProcessWaitlistSelectionCommand(
    Guid ReservationId,
    Guid SeatId,
    Guid EventId,
    string Section,
    DateTime ExpiredAt
) : IRequest<ProcessWaitlistSelectionResult?>;

public class ProcessWaitlistSelectionResult
{
    public Guid WaitlistEntryId { get; set; }
    public Guid UserId { get; set; }
    public Guid OpportunityWindowId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
