namespace Notification.Application.UseCases.SendWaitlistNotification;

public record SendWaitlistNotificationResponse(
    Guid NotificationId,
    bool Success,
    string Message
);