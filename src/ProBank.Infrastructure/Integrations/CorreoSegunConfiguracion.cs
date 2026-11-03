using Microsoft.Graph;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Configuracion;
using ProBank.Domain.Modules.Configuracion;
using ProBank.Infrastructure.Security;
using static ProBank.Domain.Modules.Configuracion.ReglasConfiguracion;

namespace ProBank.Infrastructure.Integrations;

/// <summary>
/// SQL-08 y CFG-03: en cada envío elige el proveedor de Configuración › Parámetros generales (Graph, Smtp o Carpeta, esta solo en
/// desarrollo), así un cambio guardado se aplica sin reiniciar la API.
/// </summary>
internal sealed class CorreoSegunConfiguracion(IConfiguracionGlobal configuracion, GraphServiceClient graph, ISecretProvider secretos, string carpetaLocal) : IMailSender
{
    public async Task Enviar(IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml,
        (string Nombre, byte[] Contenido, string Tipo)? adjunto, CancellationToken ct)
    {
        var v = await configuracion.Valores(ct);
        var nombre = Valor(v, NombreRemitente) ?? "ProBank";
        IMailSender proveedor = (Valor(v, Proveedor) ?? "Graph") switch
        {
            "Graph" => new GraphMailSender(graph, Valor(v, Buzon) ?? throw new InvalidOperationException("Falta el buzón de servicio en Configuración › Parámetros generales.")),
            "Smtp" => new SmtpMailSender(new OpcionesSmtp(Valor(v, SmtpServidor) ?? throw new InvalidOperationException("Falta el servidor SMTP en Configuración › Parámetros generales."),
                Entero(v, SmtpPuerto, 587), Valor(v, SmtpSeguridad) ?? "StartTls", Valor(v, SmtpUsuario), Valor(v, SmtpSecreto),
                Valor(v, SmtpRemitente) ?? throw new InvalidOperationException("Falta el remitente SMTP en Configuración › Parámetros generales."), nombre), secretos),
            "Carpeta" when configuracion.PermiteCarpeta => new CarpetaCorreo(carpetaLocal),
            var p => throw new InvalidOperationException($"El proveedor de correo {p} no está disponible en este ambiente."),
        };
        await proveedor.Enviar(destinatarios, asunto, cuerpoHtml, adjunto, ct);
    }
}
