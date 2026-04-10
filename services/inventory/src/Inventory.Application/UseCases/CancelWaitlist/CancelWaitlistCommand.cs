using MediatR;

namespace Inventory.Application.UseCases.CancelWaitlist;

public record CancelWaitlistCommand(Guid UserId, Guid EventId, string Section) : IRequest<CancelWaitlistResult>;

public class CancelWaitlistResult
{
    public Guid WaitlistEntryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
}
