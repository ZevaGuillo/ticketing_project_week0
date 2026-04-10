namespace Notification.Application.Ports;

public interface IQrCodeService
{
    byte[] GenerateBytes(string data);
}
