using System.Net;
using System.Net.Mail;
using Homi.Helpers;

namespace Homi.Services;

/// Envía el código de verificación. Si no hay servidor de correo configurado, el código se guarda en el registro (modo de prueba).
public class CorreoService
{
    private readonly IConfiguration _configuracion;

    public CorreoService(IConfiguration configuracion) => _configuracion = configuracion;

    public async Task EnviarCodigoAsync(string destino, string nombre, string codigo)
    {
        var servidor = _configuracion["Correo:Servidor"];
        if (string.IsNullOrWhiteSpace(servidor))
        {
            Registro.Info($"[MODO PRUEBA] Código de verificación para {destino}: {codigo}. Configura Correo:Servidor en appsettings.json para enviar correos reales.");
            return;
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_configuracion["Correo:Remitente"] ?? _configuracion["Correo:Usuario"]!, "Homi"),
            Subject = "Tu código de verificación de Homi",
            Body = $"Hola {nombre}. Tu código para confirmar tu correo es {codigo}. Vence en 15 minutos.",
            IsBodyHtml = false
        };
        mensaje.To.Add(destino);

        var puerto = int.TryParse(_configuracion["Correo:Puerto"], out var p) ? p : 587;
        using var cliente = new SmtpClient(servidor, puerto)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_configuracion["Correo:Usuario"], _configuracion["Correo:Clave"])
        };
        await cliente.SendMailAsync(mensaje);
    }
}
