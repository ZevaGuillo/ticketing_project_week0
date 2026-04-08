using MediatR;
using Microsoft.Extensions.Logging;
using Notification.Application.Email;
using Notification.Application.Ports;
using Notification.Domain.Entities;

namespace Notification.Application.UseCases.SendTicketNotification;

public class SendTicketNotificationCommandHandler : IRequestHandler<SendTicketNotificationCommand, SendTicketNotificationResponse>
{
    private readonly IEmailNotificationRepository _repository;
    private readonly IEmailService _emailService;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<SendTicketNotificationCommandHandler> _logger;

    public SendTicketNotificationCommandHandler(
        IEmailNotificationRepository repository,
        IEmailService emailService,
        IQrCodeService qrCodeService,
        ILogger<SendTicketNotificationCommandHandler> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    public async Task<SendTicketNotificationResponse> Handle(SendTicketNotificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing ticket notification for order {OrderId} to {Email}", request.OrderId, request.RecipientEmail);

        try
        {
            // Check if notification already exists (idempotency)
            var existingNotification = await _repository.GetByOrderIdAsync(request.OrderId);
            if (existingNotification != null)
            {
                _logger.LogInformation("Notification already exists for order {OrderId}", request.OrderId);
                return new SendTicketNotificationResponse(existingNotification.Id, true, "Notification already sent");
            }

            // Build email content
            var subject = $"Your Ticket for {request.EventName}";
            var qrBytes = !string.IsNullOrEmpty(request.QrCodeData)
                ? _qrCodeService.GenerateBytes(request.QrCodeData)
                : null;
            var body = BuildEmailBody(request, qrBytes != null && qrBytes.Length > 0);

            // Send email
            var emailSent = await _emailService.SendAsync(
                request.RecipientEmail,
                subject,
                body,
                pdfUrl: request.TicketPdfUrl,
                qrBytes: qrBytes);

            // Create and persist notification record
            var notification = new EmailNotification
            {
                Id = Guid.NewGuid(),
                TicketId = request.TicketId,
                OrderId = request.OrderId,
                RecipientEmail = request.RecipientEmail,
                Subject = subject,
                Body = body,
                TicketPdfUrl = request.TicketPdfUrl,
                Status = emailSent ? NotificationStatus.Sent : NotificationStatus.Failed,
                CreatedAt = DateTime.UtcNow,
                SentAt = emailSent ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow,
                FailureReason = emailSent ? null : "Email service failed"
            };

            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();

            if (emailSent)
                _logger.LogInformation("Notification sent and persisted for order {OrderId}", request.OrderId);
            else
                _logger.LogWarning("Notification queued but email send failed for order {OrderId}", request.OrderId);

            return new SendTicketNotificationResponse(
                notification.Id,
                true,
                emailSent ? "Notification sent successfully" : "Notification queued (email send failed)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification for order {OrderId}", request.OrderId);
            return new SendTicketNotificationResponse(Guid.Empty, false, $"Error: {ex.Message}");
        }
    }

    private string BuildEmailBody(SendTicketNotificationCommand request, bool hasQr)
    {
        return EmailTemplates.TicketConfirmation(
            request.EventName,
            request.SeatNumber,
            request.Price,
            request.Currency,
            request.TicketIssuedAt,
            hasQr);
    }
}
