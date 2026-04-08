namespace Notification.Application.Ports;

public interface IEmailService
{
    Task<bool> SendAsync(string recipientEmail, string subject, string body, string? pdfUrl = null, byte[]? qrBytes = null);
}
