using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ProBank.Application.Abstractions;
using ProBank.Infrastructure.Security;

namespace ProBank.Infrastructure.Integrations;

/// <summary>Datos SMTP de Configuración › Parámetros generales. La clave no se guarda: SecretoClave es el nombre del secreto de Key Vault.</summary>
public sealed record OpcionesSmtp(string Servidor, int Puerto, string Seguridad, string? Usuario, string? SecretoClave, string Remitente, string NombreRemitente);

/// <summary>
/// SQL-08 por SMTP (servidor de correo propio o relay), alternativa a Microsoft Graph (ADR 0011). Sin usuario envía como relay
/// (el servidor autoriza por IP); con usuario, la clave se lee de Key Vault en cada envío. La configuración ya la validó
/// ReglasConfiguracion al guardarla.
/// </summary>
internal sealed class SmtpMailSender(OpcionesSmtp opciones, ISecretProvider secretos) : IMailSender
{
    public async Task Enviar(IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml,
        (string Nombre, byte[] Contenido, string Tipo)? adjunto, CancellationToken ct)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(opciones.NombreRemitente, opciones.Remitente));
        mensaje.To.AddRange(destinatarios.Select(MailboxAddress.Parse));
        mensaje.Subject = asunto;
        var cuerpo = new BodyBuilder { HtmlBody = cuerpoHtml };
        if (adjunto is { } a) cuerpo.Attachments.Add(a.Nombre, a.Contenido, ContentType.Parse(a.Tipo));
        mensaje.Body = cuerpo.ToMessageBody();

        using var smtp = new SmtpClient { Timeout = 60_000 };
        await smtp.ConnectAsync(opciones.Servidor, opciones.Puerto, opciones.Seguridad switch
        {
            "TlsDirecto" => SecureSocketOptions.SslOnConnect,
            "Ninguna" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls,
        }, ct);
        if (!string.IsNullOrWhiteSpace(opciones.Usuario))
            await smtp.AuthenticateAsync(opciones.Usuario, await secretos.Obtener(opciones.SecretoClave!, ct), ct);
        await smtp.SendAsync(mensaje, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
