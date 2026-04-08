namespace Notification.Application.UseCases.SendTicketNotification;

public record SendTicketNotificationResponse(Guid NotificationId, bool Success, string Message);
