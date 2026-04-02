using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Notification.Application.Ports;

namespace Notification.Infrastructure.Email;

public class SmtpEmailOptions
{
    public const string Section = "Email:Smtp";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@ticketing.local";
    public string FromName { get; set; } = "Ticketing Platform";
    public bool EnableSsl { get; set; } = true;
    public bool UseDevMode { get; set; } = true;
}

public class SmtpEmailService : IEmailService
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string recipientEmail, string subject, string body, string? attachmentPath = null)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail) || !recipientEmail.Contains('@'))
        {
            _logger.LogWarning("Invalid recipient email address: {Recipient}. Skipping send.", recipientEmail);
            return false;
        }

        try
        {
            if (_options.UseDevMode)
            {
                // In dev mode, just log the email instead of sending
                _logger.LogInformation($"[DEV MODE] Email queued for sending");
                _logger.LogInformation($"  To: {recipientEmail}");
                _logger.LogInformation($"  Subject: {subject}");
                _logger.LogInformation($"  Attachment: {attachmentPath ?? "none"}");
                return true;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            var ssl = _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_options.Host, _options.Port, ssl);
            if (!string.IsNullOrEmpty(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Recipient} via {Host}:{Port}", recipientEmail, _options.Host, _options.Port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Recipient}: {Message}", recipientEmail, ex.Message);
            return false;
        }
    }
}
