using MediatR;

namespace Inventory.Application.UseCases.GetWaitlistStatus;

public record GetWaitlistStatusQuery(
    Guid UserId,
    Guid EventId,
    string Section
) : IRequest<GetWaitlistStatusResponse?>;

public class GetWaitlistStatusResponse
{
    public Guid WaitlistEntryId { get; set; }
    public int QueuePosition { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
}