using Microsoft.Extensions.Logging;
using Notification.Application.Ports;
using QRCoder;

namespace Notification.Infrastructure.Email;

public class QrCodeService : IQrCodeService
{
    private readonly ILogger<QrCodeService> _logger;

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    public byte[] GenerateBytes(string data)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(10);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for data: {Data}", data);
            return Array.Empty<byte>();
        }
    }
}
