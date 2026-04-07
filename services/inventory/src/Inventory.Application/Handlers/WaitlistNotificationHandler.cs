using Inventory.Domain.Events;
using Inventory.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Handlers;

public class WaitlistNotificationHandler
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly ILogger<WaitlistNotificationHandler> _logger;
    private const int MaxRetries = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(1);

    public WaitlistNotificationHandler(
        INotificationService notificationService,
        IUserService userService,
        ILogger<WaitlistNotificationHandler> logger)
    {
        _notificationService = notificationService;
        _userService = userService;
        _logger = logger;
    }

    public async Task Handle(WaitlistOpportunityGrantedEvent @event, CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(@event.UserId, ct);
        
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for waitlist opportunity", @event.UserId);
            return;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("User {UserId} is inactive, skipping notification", @event.UserId);
            return;
        }

        if (!user.IsEmailVerified)
        {
            _logger.LogWarning("User {UserId} email is not verified, skipping notification", @event.UserId);
            return;
        }

        var emailMessage = ComposeEmail(@event, user);
        
        await SendWithRetryAsync(emailMessage, @event, ct);
    }

    private async Task SendWithRetryAsync(EmailMessage message, WaitlistOpportunityGrantedEvent @event, CancellationToken ct)
    {
        var delay = InitialDelay;
        var lastException = string.Empty;
        
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                var sent = await _notificationService.SendEmailAsync(message, ct);
                if (sent)
                {
                    _logger.LogInformation(
                        "Email notification sent successfully for opportunity {OpportunityId} to user {UserId}",
                        @event.OpportunityId, @event.UserId);
                    return;
                }
                
                _logger.LogWarning(
                    "Failed to send email for opportunity {OpportunityId}, attempt {Attempt}/{MaxRetries}",
                    @event.OpportunityId, attempt + 1, MaxRetries);
            }
            catch (Exception ex)
            {
                lastException = ex.Message;
                _logger.LogWarning(
                    ex,
                    "Exception sending email for opportunity {OpportunityId}, attempt {Attempt}/{MaxRetries}",
                    @event.OpportunityId, attempt + 1, MaxRetries);
            }

            if (attempt < MaxRetries - 1)
            {
                await Task.Delay(delay, ct);
                delay *= 2;
            }
        }

        _logger.LogError(
            "Failed to send email notification for opportunity {OpportunityId} after {MaxRetries} attempts. Last error: {LastException}",
            @event.OpportunityId, MaxRetries, lastException);
        
        throw new InvalidOperationException(
            $"Failed to send email notification after {MaxRetries} attempts. Last error: {lastException}");
    }

    private EmailMessage ComposeEmail(WaitlistOpportunityGrantedEvent @event, UserInfo user)
    {
        var expirationTime = @event.CreatedAt.AddSeconds(@event.OpportunityTTL);
        
        return new EmailMessage
        {
            To = user.Email,
            Subject = "¡Tienes una oportunidad de compra!",
            Body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 10px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>¡Hola, {user.FullName}!</h1>
        </div>
        <div class=""content"">
            <p>¡Buenas noticias! Una oportunidad de compra está disponible para ti.</p>
            
            <h2>Detalles de la oportunidad:</h2>
            <ul>
                <li><strong>ID de oportunidad:</strong> {@event.OpportunityId}</li>
                <li><strong>Evento:</strong> {@event.EventId}</li>
                <li><strong>Asiento:</strong> Sección {@event.Section}</li>
                <li><strong>Tiempo disponible:</strong> {@event.OpportunityTTL / 60} minutos</li>
                <li><strong>Expira:</strong> {expirationTime:yyyy-MM-dd HH:mm:ss} UTC</li>
            </ul>
            
            <p>Por favor, completa tu compra antes de que expire la oportunidad.</p>
            
            <p>Saludos,<br/>El equipo de ventas</p>
        </div>
        <div class=""footer"">
            <p>Este es un mensaje automático, por favor no responds a este correo.</p>
        </div>
    </div>
</body>
</html>"
        };
    }
}
