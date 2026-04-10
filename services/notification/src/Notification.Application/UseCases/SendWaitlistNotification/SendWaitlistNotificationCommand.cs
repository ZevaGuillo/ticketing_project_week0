using MediatR;

namespace Notification.Application.UseCases.SendWaitlistNotification;

public record SendWaitlistNotificationCommand(
    Guid OpportunityId,
    Guid WaitlistEntryId,
    Guid UserId,
    string RecipientEmail,
    string EventName,
    string Section,
    int OpportunityTTL,
    DateTime CreatedAt
) : IRequest<SendWaitlistNotificationResponse>;