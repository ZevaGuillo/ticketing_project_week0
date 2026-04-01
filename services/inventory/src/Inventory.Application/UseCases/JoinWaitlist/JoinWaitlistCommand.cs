using MediatR;

namespace Inventory.Application.UseCases.JoinWaitlist;

public record JoinWaitlistCommand(
    Guid UserId,
    Guid EventId,
    string Section
) : IRequest<JoinWaitlistResponse>;

public class JoinWaitlistResponse
{
    public Guid WaitlistEntryId { get; set; }
    public int QueuePosition { get; set; }
    public DateTime JoinedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}