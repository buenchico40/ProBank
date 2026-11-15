using System.Drawing;
using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using ProBank.Application.Modules.Decisiones;
using ProBank.Contracts.Decisiones;
using ProBank.Infrastructure.Security;
using static ProBank.Infrastructure.Reportes.EstiloReporte;

namespace ProBank.Infrastructure.Reportes;

/// <summary>
/// DEC-04: PDF de una decisión con DevExpress Reporting: detalle, firmas de los aprobadores con su sello y la verificación de la
/// huella. El PDF lo firma el certificado del banco: cualquier cambio posterior al archivo invalida esa firma.
/// </summary>
internal sealed class GeneradorDecisionPdfDevExpress(CertificadoFirma certificado) : IGeneradorDecisionPdf
{
    public async Task<byte[]> Pdf(DecisionDetalleDto decision, string generadoPor, DateTime ahora, CancellationToken ct)
    {
        var cert = await certificado.Obtener(ct);
        var generado = $"Generado por {generadoPor} el {ahora:dd/MM/yyyy HH:mm}";
        using var reporte = DecisionReporte.Crear(decision, generado, cert.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.SimpleName, false), cert.Thumbprint);
        return EstiloReporte.Pdf(reporte, $"Decisión {decision.Decision.Codigo}",
            (cert, $"Decisión {decision.Decision.Codigo} · {decision.Decision.Estado} · huella {decision.Huella ?? "sin enviar"}"));
    }
}

internal static class DecisionReporte
{
    // Estilos del HTML de la descripción en el PDF (la descripción guardada no lleva estilos: los quita la limpieza)
    private const string EstiloDescripcion = "<style>div,p,li,td,th{font-family:Segoe UI,Arial,sans-serif;font-size:9.5pt;color:#1F2937}" +
        "p{margin:0 0 4pt}h2,h3{font-size:11pt;margin:6pt 0 2pt}h4{font-size:10pt;margin:6pt 0 2pt}" +
        "table{border-collapse:collapse;margin:4pt 0}td,th{border:1px solid #D9DEE5;padding:2pt 6pt;text-align:left}th{background:#F8FAFC}</style>";

    private static string Tamano(long b) => b >= 1048576 ? (b / 1048576d).ToString("0.0", Es) + " MB" : Math.Max(1, b / 1024).ToString(Es) + " KB";
    private static string Fecha(DateTime? f) => f is null ? "—" : f.Value.ToString("dd/MM/yyyy HH:mm", Es);

    public static XtraReport Crear(DecisionDetalleDto x, string generado, string certificado, string huellaCertificado)
    {
        var d = x.Decision;
        var r = Nuevo(d.Codigo);
        var firmadas = x.Aprobadores.Count(a => a.Resultado != "Pendiente");
        r.Bands.Add(Encabezado(d.Titulo, $"Versión {d.Version}   ·   {d.Tipo}   ·   {d.Area}   ·   {d.Fecha.ToString("dd/MM/yyyy", Es)}   ·   {generado}", d.Codigo,
        [
            ("Estado", d.Estado, d.FechaCierre is null ? (d.FechaEnvio is null ? "Sin enviar" : "Enviada el " + Fecha(d.FechaEnvio)) : "Cerrada el " + Fecha(d.FechaCierre)),
            ("Archivos", x.Adjuntos.Count.ToString(Es), x.Adjuntos.Count == 0 ? "Sin archivos de respaldo" : "Fijos desde el envío"),
            ("Aprobaciones", $"{d.Aprobadas} de {d.TotalAprobadores}", d.Secuencial ? "En secuencia" : "Todos a la vez"),
            ("Firmas", $"{firmadas} de {x.Aprobadores.Count}", "Con clave del aprobador"),
            ("Integridad", d.FechaEnvio is null ? "—" : x.Integra ? "Íntegra" : "Alterada", "Huella SHA-256 del contenido"),
        ]));
        r.Bands.Add(EncabezadoPagina($"ProBank · Decisión {d.Codigo}", d.Titulo));
        r.Bands.Add(new DetailBand { HeightF = 0 });
        r.Bands.Add(PiePagina($"{d.Codigo} · {generado} · documento firmado digitalmente"));

        var n = 0;
        r.Bands.Add(Seccion(n++, "Detalle de la decisión", null, b =>
        {
            (string, string)[] datos =
            [
                ("Tipo", $"{d.Tipo} ({d.TipoCodigo})"), ("Área", d.Area), ("Fecha de la decisión", d.Fecha.ToString("dd/MM/yyyy", Es)),
                ("Registrada por", $"{d.CreadoPor} · {Fecha(d.FechaRegistro)}"), ("Enviada a aprobación", Fecha(d.FechaEnvio)), ("Cierre", Fecha(d.FechaCierre)),
            ];
            for (var i = 0; i < datos.Length; i++)
            {
                var (col, fila) = (i % 3, i / 3);
                b.Controls.Add(Etiqueta(datos[i].Item1.ToUpper(Es), new PointF(col * 363, fila * 40), new SizeF(350, 14), F(7.5f, DXFontStyle.Bold), Gris));
                b.Controls.Add(Etiqueta(datos[i].Item2, new PointF(col * 363, fila * 40 + 14), new SizeF(350, 18), F(9.5f), Tinta));
            }
            var y = 86f;
            if (x.MotivoAnulacion is not null)
            {
                b.Controls.Add(Etiqueta("Motivo de la anulación: " + x.MotivoAnulacion, new PointF(0, y), new SizeF(Ancho, 20), F(9.5f, DXFontStyle.Bold), Rojo));
                y += 26;
            }
            b.Controls.Add(Etiqueta("DESCRIPCIÓN", new PointF(0, y), new SizeF(Ancho, 14), F(7.5f, DXFontStyle.Bold), Gris));
            // HTML del editor enriquecido (ya limpio al guardarlo): títulos, negritas, listas y tablas con su formato
            b.Controls.Add(new XRRichText
            {
                LocationF = new PointF(0, y + 16), SizeF = new SizeF(Ancho, 20), CanGrow = true, Font = F(9.5f), ForeColor = Tinta,
                Html = EstiloDescripcion + $"<div>{x.Descripcion}</div>",
            });
        }, x.MotivoAnulacion is null ? 126 : 152));

        r.Bands.Add(Tabla(n++, "Archivos de respaldo", "La decisión no tiene archivos de respaldo.", false,
            x.Adjuntos.Select(a => new FilaArchivo(a.Nombre, Tamano(a.Tamano), Fecha(a.Fecha), a.Hash)).ToList(),
            [("Archivo", "Nombre", 420), ("Tamaño", "Tamano", 90), ("Agregado", "Agregado", 120), ("Huella SHA-256", "Hash", 459)]));

        r.Bands.Add(Tabla(n++, "Aprobaciones y firmas", "La decisión aún no se envió a aprobación.", false,
            x.Aprobadores.Select(a => new FilaFirma(a.Orden.ToString(Es), $"{a.Nombre} ({a.Usuario})" + (a.Origen == "Agregado" ? " · agregado" : ""), a.Resultado, Fecha(a.Fecha), a.Ip ?? "—",
                a.Comentario ?? "—", a.Sello is null ? "—" : a.Sello + (a.SelloValido == false ? "  (NO COINCIDE)" : ""))).ToList(),
            [("Orden", "Orden", 45), ("Aprobador", "Aprobador", 185), ("Resultado", "Estado", 85), ("Fecha y hora", "Fecha", 100), ("IP", "Ip", 85),
             ("Comentario", "Comentario", 254), ("Sello SHA-256", "Sello", 335)]));

        r.Bands.Add(Tabla(n++, "Notificados", "La decisión no tiene notificados.", false,
            x.Notificados.Select(p => new FilaNotificado($"{p.Nombre} ({p.Usuario})", Fecha(p.Desde), p.AgregadoPor, Fecha(p.UltimaConsulta))).ToList(),
            [("Notificado", "Persona", 400), ("Desde", "Desde", 200), ("Agregado por", "AgregadoPor", 289), ("Última consulta", "Consulta", 200)]));

        if (x.Versiones.Count > 1 || x.FirmasAnteriores.Count > 0)
        {
            r.Bands.Add(Tabla(n++, "Versiones del contenido", "—", false,
                x.Versiones.Select(v => new FilaVersion(v.Version.ToString(Es), Fecha(v.FechaRegistro), v.RegistradaPor, v.Motivo ?? "—", v.Titulo, v.Huella)).ToList(),
                [("Versión", "Version", 60), ("Fecha", "Fecha", 100), ("Registrada por", "Por", 150), ("Motivo", "Motivo", 230), ("Título", "Titulo", 290),
                 ("Huella SHA-256", "Huella", 259)]));
            r.Bands.Add(Tabla(n++, "Firmas de versiones anteriores", "No hay firmas de versiones anteriores.", false,
                x.FirmasAnteriores.Select(f => new FilaFirmaAnterior(f.Version.ToString(Es), $"{f.Nombre} ({f.Usuario})", f.Resultado, Fecha(f.Fecha),
                    f.Comentario ?? "—", f.Sello + (f.SelloValido ? "" : "  (NO COINCIDE)"))).ToList(),
                [("Versión", "Version", 60), ("Aprobador", "Aprobador", 190), ("Resultado", "Estado", 85), ("Fecha y hora", "Fecha", 100), ("Comentario", "Comentario", 299),
                 ("Sello SHA-256", "Sello", 355)]));
        }

        r.Bands.Add(Tabla(n++, "Trazabilidad", "Sin eventos.", true,
            x.Eventos.Select(e => new FilaEvento(Fecha(e.Fecha), $"{e.Nombre} ({e.Usuario})", e.Evento, e.Detalle ?? "—", e.Ip ?? "—")).ToList(),
            [("Fecha y hora", "Fecha", 110), ("Usuario", "Usuario", 210), ("Evento", "Evento", 160), ("Detalle", "Detalle", 509), ("IP", "Ip", 100)]));

        r.Bands.Add(Seccion(n, "Verificación", null, b =>
        {
            string[] lineas =
            [
                $"Huella de la versión {d.Version}: {x.Huella ?? "— (sin enviar)"}",
                $"Huella del contenido hoy: {x.HuellaActual}   ·   {(d.FechaEnvio is null ? "sin enviar" : x.Integra ? "coinciden: el contenido no cambió" : "NO COINCIDEN: el contenido cambió")}",
                "Cada firma es la del aprobador en ProBank: confirmó su identidad con su clave y su sello SHA-256 une la huella de la información registrada en la versión con su usuario, su decisión, la fecha y hora y la IP. Si un aprobador modifica la decisión, nace una versión nueva y todos vuelven a firmar.",
                $"Este PDF está firmado digitalmente con el certificado «{certificado}» (huella {huellaCertificado}); si el archivo se modifica, el lector de PDF lo indica.",
            ];
            for (var i = 0; i < lineas.Length; i++)
                b.Controls.Add(Etiqueta(lineas[i], new PointF(0, i * 22), new SizeF(Ancho, 20), F(8.5f), i == 1 && !x.Integra && d.FechaEnvio is not null ? Rojo : Tinta));
        }, 92));
        return r;
    }

    private sealed record FilaFirma(string Orden, string Aprobador, string Estado, string Fecha, string Ip, string Comentario, string Sello);
    private sealed record FilaNotificado(string Persona, string Desde, string AgregadoPor, string Consulta);
    private sealed record FilaVersion(string Version, string Fecha, string Por, string Motivo, string Titulo, string Huella);
    private sealed record FilaArchivo(string Nombre, string Tamano, string Agregado, string Hash);
    private sealed record FilaFirmaAnterior(string Version, string Aprobador, string Estado, string Fecha, string Comentario, string Sello);
    private sealed record FilaEvento(string Fecha, string Usuario, string Evento, string Detalle, string Ip);
}
