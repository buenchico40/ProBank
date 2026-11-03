using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using ProBank.Application.Abstractions;

namespace ProBank.Infrastructure.Integrations;

/// <summary>
/// SQL-08: envía correo con Microsoft Graph desde el buzón de servicio (Configuración › Parámetros generales).
/// El permiso Mail.Send de la aplicación se limita a ese buzón con una Application Access Policy (ADR 0011).
/// </summary>
internal sealed class GraphMailSender(GraphServiceClient graph, string buzon) : IMailSender
{
    public async Task Enviar(IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml,
        (string Nombre, byte[] Contenido, string Tipo)? adjunto, CancellationToken ct)
    {
        var mensaje = new Message
        {
            Subject = asunto,
            Body = new ItemBody { ContentType = BodyType.Html, Content = cuerpoHtml },
            ToRecipients = destinatarios.Select(d => new Recipient { EmailAddress = new EmailAddress { Address = d } }).ToList(),
        };
        if (adjunto is { } a)
            mensaje.Attachments = [new FileAttachment { Name = a.Nombre, ContentBytes = a.Contenido, ContentType = a.Tipo }];

        await graph.Users[buzon].SendMail.PostAsync(new SendMailPostRequestBody { Message = mensaje, SaveToSentItems = true }, cancellationToken: ct);
    }
}
