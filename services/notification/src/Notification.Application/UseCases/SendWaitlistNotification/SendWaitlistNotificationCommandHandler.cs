using MediatR;
using Microsoft.Extensions.Logging;
using Notification.Application.Email;
using Notification.Application.Ports;
using Notification.Domain.Entities;

namespace Notification.Application.UseCases.SendWaitlistNotification;

public class SendWaitlistNotificationCommandHandler : IRequestHandler<SendWaitlistNotificationCommand, SendWaitlistNotificationResponse>
{
    private readonly IEmailNotificationRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWaitlistNotificationCommandHandler> _logger;

    public SendWaitlistNotificationCommandHandler(
        IEmailNotificationRepository repository,
        IEmailService emailService,
        ILogger<SendWaitlistNotificationCommandHandler> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<SendWaitlistNotificationResponse> Handle(SendWaitlistNotificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing waitlist notification for opportunity {OpportunityId} to {Email}", 
            request.OpportunityId, request.RecipientEmail);

        try
        {
            var existingNotification = await _repository.GetByOpportunityIdAsync(request.OpportunityId);
            if (existingNotification != null)
            {
                _logger.LogInformation("Notification already exists for opportunity {OpportunityId}", request.OpportunityId);
                return new SendWaitlistNotificationResponse(existingNotification.Id, true, "Notification already sent");
            }

            var expiresAt = request.CreatedAt.AddSeconds(request.OpportunityTTL);
            var purchaseUrl = $"http://localhost:3000/events/{request.EventName}";
            var subject = "¡Tienes una oportunidad de compra!";
            var body = EmailTemplates.WaitlistOpportunity(
                request.RecipientEmail, 
                request.Section, 
                expiresAt, 
                request.EventName, 
                purchaseUrl);

            var emailSent = await _emailService.SendAsync(
                request.RecipientEmail,
                subject,
                body);

            var notification = new EmailNotification
            {
                Id = Guid.NewGuid(),
                OpportunityId = request.OpportunityId,
                RecipientEmail = request.RecipientEmail,
                Subject = subject,
                Body = body,
                Status = emailSent ? NotificationStatus.Sent : NotificationStatus.Failed,
                FailureReason = emailSent ? null : "Email service failed",
                CreatedAt = DateTime.UtcNow,
                SentAt = emailSent ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();

            if (emailSent)
                _logger.LogInformation("Waitlist notification sent and persisted for opportunity {OpportunityId}", request.OpportunityId);
            else
                _logger.LogWarning("Waitlist notification queued but email send failed for opportunity {OpportunityId}", request.OpportunityId);

            return new SendWaitlistNotificationResponse(
                notification.Id,
                true,
                emailSent ? "Notification sent successfully" : "Notification queued (email send failed)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing waitlist notification for opportunity {OpportunityId}", request.OpportunityId);
            return new SendWaitlistNotificationResponse(Guid.Empty, false, $"Error: {ex.Message}");
        }
    }
}