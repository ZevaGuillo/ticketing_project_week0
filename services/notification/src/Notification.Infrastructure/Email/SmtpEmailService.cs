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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpEmailOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        string recipientEmail,
        string subject,
        string body,
        string? pdfUrl = null,
        byte[]? qrBytes = null)
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
                _logger.LogInformation("[DEV MODE] Email queued for sending");
                _logger.LogInformation("  To: {Recipient}", recipientEmail);
                _logger.LogInformation("  Subject: {Subject}", subject);
                _logger.LogInformation("  PDF attachment: {Pdf}", pdfUrl ?? "none");
                _logger.LogInformation("  QR image: {Qr}", qrBytes?.Length > 0 ? $"{qrBytes.Length} bytes" : "none");
                return true;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = subject;

            var builder = new BodyBuilder();

            // Embed QR as CID inline image (supported by all major email clients)
            if (qrBytes != null && qrBytes.Length > 0)
            {
                var qrImage = builder.LinkedResources.Add("qrcode.png", qrBytes, new MimeKit.ContentType("image", "png"));
                qrImage.ContentId = "qrcode";
                qrImage.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            }

            builder.HtmlBody = body;

            // Download and attach PDF
            if (!string.IsNullOrEmpty(pdfUrl))
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient("gateway");
                    var pdfResponse = await httpClient.GetAsync(pdfUrl);
                    if (pdfResponse.IsSuccessStatusCode)
                    {
                        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
                        builder.Attachments.Add("ticket.pdf", pdfBytes, new MimeKit.ContentType("application", "pdf"));
                        _logger.LogInformation("PDF attached from {Url}", pdfUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Could not download PDF from {Url}: {Status}", pdfUrl, pdfResponse.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to attach PDF from {Url}", pdfUrl);
                }
            }

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
