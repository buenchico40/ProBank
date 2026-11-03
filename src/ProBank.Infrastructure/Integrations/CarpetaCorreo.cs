using System.Text;
using ProBank.Application.Abstractions;

namespace ProBank.Infrastructure.Integrations;

/// <summary>
/// Solo desarrollo (Correo:CarpetaLocal): en lugar de enviar con Graph deja cada correo como un .eml en la carpeta, con su
/// adjunto, para revisar los envíos programados sin un buzón de servicio.
/// </summary>
internal sealed class CarpetaCorreo(string carpeta) : IMailSender
{
    public async Task Enviar(IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml,
        (string Nombre, byte[] Contenido, string Tipo)? adjunto, CancellationToken ct)
    {
        Directory.CreateDirectory(carpeta);
        const string limite = "probank-limite";
        var sb = new StringBuilder()
            .AppendLine("From: probank-notificaciones@local")
            .AppendLine("To: " + string.Join(", ", destinatarios))
            .AppendLine("Subject: =?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(asunto)) + "?=")
            .AppendLine("Date: " + DateTimeOffset.Now.ToString("r"))
            .AppendLine("MIME-Version: 1.0")
            .AppendLine($"Content-Type: multipart/mixed; boundary=\"{limite}\"").AppendLine()
            .AppendLine("--" + limite).AppendLine("Content-Type: text/html; charset=utf-8").AppendLine("Content-Transfer-Encoding: base64").AppendLine()
            .AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(cuerpoHtml), Base64FormattingOptions.InsertLineBreaks));
        if (adjunto is { } a)
            sb.AppendLine("--" + limite).AppendLine($"Content-Type: {a.Tipo}; name=\"{a.Nombre}\"").AppendLine("Content-Transfer-Encoding: base64")
              .AppendLine($"Content-Disposition: attachment; filename=\"{a.Nombre}\"").AppendLine()
              .AppendLine(Convert.ToBase64String(a.Contenido, Base64FormattingOptions.InsertLineBreaks));
        sb.AppendLine("--" + limite + "--");
        var nombre = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml";
        await File.WriteAllTextAsync(Path.Combine(carpeta, nombre), sb.ToString(), ct);
    }
}
