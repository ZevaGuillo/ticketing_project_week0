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
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public string Section { get; set; } = string.Empty;
    public int QueuePosition { get; set; }
    public DateTime JoinedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}