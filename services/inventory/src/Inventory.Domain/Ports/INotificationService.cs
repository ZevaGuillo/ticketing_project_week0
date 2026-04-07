namespace Inventory.Domain.Ports;

public interface INotificationService
{
    Task<bool> SendEmailAsync(EmailMessage message, CancellationToken ct = default);
}

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
