using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.UseCases.SendTicketNotification;
using Notification.Domain.Events;

namespace Notification.Infrastructure.Messaging.Strategies;

public class TicketIssuedStrategy : INotificationEventStrategy
{
    public string Topic => "ticket-issued";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketIssuedStrategy>>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var ticketEvent = JsonSerializer.Deserialize<TicketIssuedEvent>(
            root.GetRawText(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (ticketEvent == null)
        {
            logger.LogWarning("Failed to deserialize ticket-issued event");
            return;
        }

        var command = new SendTicketNotificationCommand(
            TicketId: ticketEvent.TicketId,
            OrderId: ticketEvent.OrderId,
            RecipientEmail: ticketEvent.CustomerEmail,
            EventName: ticketEvent.EventName,
            SeatNumber: ticketEvent.SeatNumber,
            Price: ticketEvent.Price,
            TicketIssuedAt: ticketEvent.IssuedAt,
            Currency: ticketEvent.Currency,
            TicketPdfUrl: ticketEvent.TicketPdfUrl,
            QrCodeData: ticketEvent.QrCodeData);

        var result = await mediator.Send(command, ct);

        if (result.Success)
            logger.LogInformation("Notification sent for ticket {TicketId}", ticketEvent.TicketId);
        else
            logger.LogError("Failed to send notification for ticket {TicketId}: {Message}", ticketEvent.TicketId, result.Message);
    }
}
