namespace Notification.Application.Email;

public static class EmailTemplates
{
    public static string WaitlistOpportunity(
        string fullName,
        string section,
        DateTime expiresAt,
        string eventName,
        string purchaseUrl)
    {
        var minutesLeft = (int)Math.Max(1, (expiresAt - DateTime.UtcNow).TotalMinutes);
        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <style>
    body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
    .wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,.12); }}
    .header {{ background-color: #16a34a; color: #ffffff; padding: 32px 24px; text-align: center; }}
    .header h1 {{ margin: 0; font-size: 26px; }}
    .header p {{ margin: 8px 0 0; font-size: 15px; opacity: .9; }}
    .body {{ padding: 32px 24px; color: #374151; }}
    .body p {{ margin: 0 0 16px; line-height: 1.6; }}
    .timer {{ background: #fef9c3; border: 1px solid #fde047; border-radius: 6px; padding: 16px; text-align: center; margin: 24px 0; }}
    .timer span {{ font-size: 28px; font-weight: bold; color: #854d0e; }}
    .timer small {{ display: block; color: #713f12; margin-top: 4px; }}
    .btn {{ display: block; width: fit-content; margin: 0 auto; background-color: #16a34a; color: #ffffff !important; text-decoration: none; padding: 14px 36px; border-radius: 6px; font-size: 16px; font-weight: bold; }}
    .footer {{ padding: 20px 24px; text-align: center; font-size: 12px; color: #9ca3af; border-top: 1px solid #e5e7eb; }}
  </style>
</head>
<body>
  <div class=""wrapper"">
    <div class=""header"">
      <h1>¡Tienes una oportunidad de compra!</h1>
      <p>Un asiento está disponible para ti</p>
    </div>
    <div class=""body"">
      <p>Hola <strong>{fullName}</strong>,</p>
      <p>¡Buenas noticias! Un asiento en la sección <strong>{section}</strong> para <strong>{eventName}</strong> está ahora disponible para ti como parte de la lista de espera.</p>
      <div class=""timer"">
        <span>{minutesLeft} min</span>
        <small>Tiempo restante para completar tu compra<br/>(expira a las {expiresAt:HH:mm} UTC)</small>
      </div>
      <p>Haz clic en el botón de abajo para reservar tu asiento antes de que expire la ventana:</p>
      <a href=""{purchaseUrl}"" class=""btn"">Reservar mi asiento</a>
      <p style=""margin-top:24px; font-size:13px; color:#6b7280;"">Si no solicitaste unirte a la lista de espera, ignora este correo.</p>
    </div>
    <div class=""footer"">
      &copy; Ticketing Platform. Este es un correo automático, por favor no respondas.
    </div>
  </div>
</body>
</html>";
    }

    public static string TicketConfirmation(
        string eventName,
        string seatNumber,
        decimal price,
        string currency,
        DateTime issuedAt,
        bool hasQr = false)
    {
        var qrSection = hasQr
            ? @"<div class=""qr-box"">
        <p style=""margin:0 0 12px; font-weight:bold; color:#374151;"">Código QR de tu ticket</p>
        <img src=""cid:qrcode"" alt=""QR Code"" width=""160"" height=""160"" style=""display:block;margin:0 auto;"" />
      </div>"
            : string.Empty;
        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <style>
    body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
    .wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,.12); }}
    .header {{ background-color: #2563eb; color: #ffffff; padding: 32px 24px; text-align: center; }}
    .header h1 {{ margin: 0; font-size: 26px; }}
    .header p {{ margin: 8px 0 0; font-size: 15px; opacity: .9; }}
    .body {{ padding: 32px 24px; color: #374151; }}
    .body p {{ margin: 0 0 16px; line-height: 1.6; }}
    .detail-box {{ background: #f0f9ff; border: 1px solid #bae6fd; border-radius: 6px; padding: 20px; margin: 20px 0; }}
    .detail-box table {{ width: 100%; border-collapse: collapse; }}
    .detail-box td {{ padding: 8px 4px; font-size: 15px; }}
    .detail-box td:first-child {{ color: #6b7280; width: 40%; }}
    .detail-box td:last-child {{ font-weight: bold; color: #1e3a5f; }}
    .qr-box {{ background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 6px; padding: 20px; margin: 20px 0; text-align: center; }}
    .btn {{ display: inline-block; background-color: #2563eb; color: #ffffff !important; text-decoration: none; padding: 12px 32px; border-radius: 6px; font-size: 15px; font-weight: bold; }}
    .footer {{ padding: 20px 24px; text-align: center; font-size: 12px; color: #9ca3af; border-top: 1px solid #e5e7eb; }}
  </style>
</head>
<body>
  <div class=""wrapper"">
    <div class=""header"">
      <h1>¡Tu compra fue exitosa!</h1>
      <p>Aquí está tu confirmación de ticket</p>
    </div>
    <div class=""body"">
      <p>Gracias por tu compra. Tu ticket ha sido emitido exitosamente.</p>
      <div class=""detail-box"">
        <table>
          <tr><td>Evento</td><td>{eventName}</td></tr>
          <tr><td>Asiento</td><td>{seatNumber}</td></tr>
          <tr><td>Precio</td><td>{price:F2} {currency}</td></tr>
          <tr><td>Emitido</td><td>{issuedAt:dd/MM/yyyy HH:mm} UTC</td></tr>
        </table>
      </div>
      {qrSection}
      <p style=""margin-top:24px; font-size:13px; color:#6b7280;"">Si tienes alguna pregunta, contacta a nuestro equipo de soporte.</p>
    </div>
    <div class=""footer"">
      &copy; Ticketing Platform. Este es un correo automático, por favor no respondas.
    </div>
  </div>
</body>
</html>";
    }
}
