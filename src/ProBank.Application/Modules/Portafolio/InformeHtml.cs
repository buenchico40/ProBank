using System.Globalization;
using System.Net;
using System.Text;
using ProBank.Contracts.Portafolio;

namespace ProBank.Application.Modules.Portafolio;

/// <summary>
/// Informe de seguimiento como HTML (mismas clases del visor del prototipo: vw-h, vw-t, vw-f). Todo texto va codificado.
/// </summary>
internal static class InformeHtml
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-EC");
    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "—");
    private static string F(DateTime d) => d.ToString("dd/MM/yyyy", Es);
    private static string F(DateOnly d) => d.ToString("dd/MM/yyyy", Es);

    public static string Armar(InformeContenidoDto i, InformeDatos d, string generadoPor, DateTime ahora)
    {
        var h = new StringBuilder();
        h.Append("<div class=\"vw-h\"><span class=\"vw-logo\">ProBank</span><div><b>Informe ").Append(E(i.Tipo.ToLowerInvariant()))
         .Append(" de seguimiento · ").Append(E(i.Codigo)).Append("</b><br><span>Periodo del ").Append(F(i.Desde)).Append(" al ").Append(F(i.Hasta))
         .Append(" · generado el ").Append(ahora.ToString("dd/MM/yyyy HH:mm", Es)).Append(" por ").Append(E(generadoPor)).Append("</span></div></div>");

        h.Append("<h3>Resumen por proyecto, mejora o bug</h3>");
        if (d.Resumen.Count == 0) h.Append("<p>Sin actividad en el periodo.</p>");
        else
        {
            h.Append("<table class=\"vw-t\"><tr><th>Ítem</th><th>Título</th><th>Tipo</th><th>Estado</th><th>Avance</th><th>Tareas con movimiento</th><th>Acuerdos</th><th>Cumplidos</th><th>Vencidos</th></tr>");
            foreach (var r in d.Resumen)
                h.Append("<tr><td>").Append(E(r.Item)).Append("</td><td>").Append(E(r.Titulo)).Append("</td><td>").Append(E(r.Tipo)).Append("</td><td>").Append(E(r.Estado))
                 .Append("</td><td>").Append(r.Avance).Append(" % (").Append(r.Completadas).Append(" de ").Append(r.Tareas).Append(")</td><td>").Append(r.TareasConMovimiento)
                 .Append("</td><td>").Append(r.Acuerdos).Append("</td><td>").Append(r.Cumplidos).Append("</td><td>").Append(r.Vencidos).Append("</td></tr>");
            h.Append("</table>");
        }

        h.Append("<h3>Bitácora de tareas</h3>");
        if (d.Bitacora.Count == 0) h.Append("<p>Sin movimientos de tareas en el periodo.</p>");
        else
        {
            h.Append("<table class=\"vw-t\"><tr><th>Fecha</th><th>Ítem</th><th>Tarea</th><th>Evento</th><th>Detalle</th><th>Usuario</th></tr>");
            foreach (var b in d.Bitacora)
                h.Append("<tr><td>").Append(b.Fecha.ToString("dd/MM/yyyy HH:mm", Es)).Append("</td><td>").Append(E(b.Item)).Append("</td><td>").Append(E(b.Tarea)).Append(" · ").Append(E(b.Titulo))
                 .Append("</td><td>").Append(E(b.Evento)).Append("</td><td>").Append(E(b.Detalle)).Append("</td><td>").Append(E(b.Usuario)).Append("</td></tr>");
            h.Append("</table>");
        }

        h.Append("<h3>Acuerdos semanales</h3>");
        if (d.Acuerdos.Count == 0) h.Append("<p>Sin acuerdos en el periodo.</p>");
        else
        {
            h.Append("<table class=\"vw-t\"><tr><th>Revisión</th><th>Ítem</th><th>Acuerdo</th><th>Descripción</th><th>Responsable</th><th>Cumplir hasta</th><th>Estado al cierre</th></tr>");
            foreach (var a in d.Acuerdos)
                h.Append("<tr><td>").Append(F(a.FechaRevision)).Append("</td><td>").Append(E(a.Item)).Append("</td><td>").Append(E(a.Codigo)).Append("</td><td>").Append(E(a.Descripcion))
                 .Append("</td><td>").Append(E(a.Responsable)).Append("</td><td>").Append(F(a.FechaCumplimiento)).Append("</td><td>").Append(E(a.Estado)).Append("</td></tr>");
            h.Append("</table>");
        }

        if (d.Temas.Count > 0)
        {
            h.Append("<h3>Temas tratados en las revisiones</h3><table class=\"vw-t\"><tr><th>Fecha</th><th>Ítem</th><th>Temas</th></tr>");
            foreach (var t in d.Temas) h.Append("<tr><td>").Append(F(t.Fecha)).Append("</td><td>").Append(E(t.Item)).Append("</td><td>").Append(E(t.Temas)).Append("</td></tr>");
            h.Append("</table>");
        }
        h.Append("<p class=\"vw-f\">Avance del ítem = tareas completadas ÷ total de tareas. Los acuerdos pendientes con la fecha de cumplimiento pasada figuran como vencidos.</p>");
        return h.ToString();
    }
}
