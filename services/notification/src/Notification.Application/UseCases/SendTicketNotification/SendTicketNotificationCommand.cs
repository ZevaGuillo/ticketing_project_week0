using MediatR;

namespace Notification.Application.UseCases.SendTicketNotification;

public record SendTicketNotificationCommand(
    Guid TicketId,
    Guid OrderId,
    string RecipientEmail,
    string EventName,
    string SeatNumber,
    decimal Price,
    DateTime TicketIssuedAt,
    string Currency = "USD",
    string? TicketPdfUrl = null,
    string? QrCodeData = null) : IRequest<SendTicketNotificationResponse>;
