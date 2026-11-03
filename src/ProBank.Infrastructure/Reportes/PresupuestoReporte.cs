using System.Drawing;
using DevExpress.Utils;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.UI;
using ProBank.Application.Modules.Presupuesto;
using static ProBank.Infrastructure.Reportes.EstiloReporte;

namespace ProBank.Infrastructure.Reportes;

/// <summary>
/// Reporte de presupuesto de un área y un año con DevExpress Reporting: indicadores, ejecución por partida y pagos por mes,
/// y luego el detalle de partidas, servicios y pagos con sus totales. Se exporta a PDF.
/// </summary>
internal sealed class GeneradorReportePresupuestoDevExpress : IGeneradorReportePresupuesto
{
    public byte[] Pdf(ReportePresupuestoModelo modelo)
    {
        using var reporte = PresupuestoReporte.Crear(modelo);
        return EstiloReporte.Pdf(reporte, modelo.Titulo);
    }
}

internal static class PresupuestoReporte
{
    private static readonly Color Disponible = ColorTranslator.FromHtml("#CBD5E1");
    private static string D(decimal v) => ReportePresupuestoModelo.Dinero(v);

    public static XtraReport Crear(ReportePresupuestoModelo m)
    {
        var r = Nuevo(m.Codigo);
        r.Bands.Add(Encabezado(m.Titulo, m.Subtitulo + "   ·   " + m.Generado, m.Codigo, m.Indicadores.Select(k => (k.Titulo, k.Valor, k.Nota)).ToList()));
        r.Bands.Add(EncabezadoPagina($"ProBank · {m.Titulo}", m.Codigo));
        r.Bands.Add(new DetailBand { HeightF = 0 });
        r.Bands.Add(PiePagina($"{m.Codigo} · {m.Generado}"));

        var n = 0;
        r.Bands.Add(Seccion(n++, "Panorama del año", null, b =>
        {
            b.Controls.Add(GraficoPartidas(m, new PointF(0, 0), new SizeF(650, 250)));
            b.Controls.Add(GraficoMeses(m, new PointF(666, 0), new SizeF(Ancho - 666, 250)));
        }, 258));

        var ps = m.Partidas;
        r.Bands.Add(Tabla(n++, "Partidas: presupuestado y ejecución", "El área no tiene partidas en el año.", false,
            ps.Select(p => new FilaPartida(p.Codigo, p.Rubro, p.Estado, D(p.Asignado), p.Ajustes == 0 ? "—" : D(p.Ajustes), D(p.Asignado + p.Ajustes),
                D(p.Comprometido), D(p.Pagado), D(p.Disponible), p.Ejecucion + " %")).ToList(),
            [("Partida", "Codigo", 80), ("Rubro", "Rubro", 229), ("Estado", "Estado", 80), ("Asignado", "Asignado", 100), ("Ajustes", "Ajustes", 90),
             ("Presupuestado", "Presupuestado", 110), ("Comprometido", "Comprometido", 100), ("Pagado", "Pagado", 100), ("Disponible", "Disponible", 110), ("Ejecución", "Ejecucion", 90)],
            new Dictionary<string, string>
            {
                ["Asignado"] = D(ps.Sum(p => p.Asignado)), ["Ajustes"] = D(ps.Sum(p => p.Ajustes)), ["Presupuestado"] = D(ps.Sum(p => p.Asignado + p.Ajustes)),
                ["Comprometido"] = D(ps.Sum(p => p.Comprometido)), ["Pagado"] = D(ps.Sum(p => p.Pagado)), ["Disponible"] = D(ps.Sum(p => p.Disponible)),
            }));

        var ss = m.Servicios;
        r.Bands.Add(Tabla(n++, "Servicios: estimado frente a pagado", "No hay servicios del área en el año.", true,
            ss.Select(x => new FilaServicio(x.Servicio.Codigo, x.Servicio.Servicio, x.Servicio.Proveedor, x.Servicio.Vence.ToString("dd/MM/yyyy", Es),
                x.Servicio.Partida ?? "—", x.Servicio.Presupuestado ? "Presupuestado" : "No presupuestado",
                D(x.Servicio.MontoEstimado), D(x.Servicio.Pagado), D(x.Diferencia), x.Estado)).ToList(),
            [("Renovación", "Codigo", 75), ("Servicio", "Servicio", 200), ("Proveedor", "Proveedor", 140), ("Vence", "Vence", 80), ("Partida", "Partida", 85),
             ("Presupuesto", "TipoPresupuesto", 105), ("Estimado", "Estimado", 100), ("Pagado", "Pagado", 100), ("Por pagar", "Diferencia", 94), ("Estado", "Estado", 110)],
            new Dictionary<string, string>
            {
                ["Estimado"] = D(ss.Sum(x => x.Servicio.MontoEstimado)), ["Pagado"] = D(ss.Sum(x => x.Servicio.Pagado)), ["Diferencia"] = D(ss.Sum(x => x.Diferencia)),
            }));

        var pg = m.Pagos;
        r.Bands.Add(Tabla(n++, "Pagos del año", "No hay pagos del área en el año.", false,
            pg.Select(x => new FilaPago(x.Pago.Codigo, x.Pago.Fecha.ToString("dd/MM/yyyy", Es), x.Pago.Solicitud,
                x.Pago.Servicio is null ? x.Pago.Descripcion : $"{x.Pago.Descripcion} (servicio: {x.Pago.Servicio})",
                x.Pago.Proveedor, x.Pago.Partidas ?? "—", x.Pago.Presupuestado ? "Presupuestado" : "No presupuestado", D(x.Pago.Monto), x.Estado)).ToList(),
            [("Pago", "Codigo", 75), ("Fecha", "Fecha", 75), ("Solicitud", "Solicitud", 75), ("Descripción", "Descripcion", 255), ("Proveedor", "Proveedor", 140),
             ("Partidas", "Partidas", 120), ("Presupuesto", "TipoPresupuesto", 104), ("Monto", "Monto", 125), ("Estado", "Estado", 120)],
            new Dictionary<string, string>
            {
                ["Monto"] = D(pg.Sum(x => x.Pago.Monto)),
                ["Descripcion"] = $"Pagado {D(pg.Where(x => x.Estado == "Pagado").Sum(x => x.Pago.Monto))} · por pagar {D(pg.Where(x => x.Estado != "Pagado").Sum(x => x.Pago.Monto))}",
            }));

        r.Bands.Add(Nota("Presupuestado = asignado + ajustes aprobados. Disponible = presupuestado − comprometido − pagado. Ejecución = (pagado + comprometido) ÷ presupuestado. " +
                         "Los servicios y pagos no presupuestados no tienen partida; el monto de un pago es lo distribuido a las partidas del área."));
        return r;
    }

    // ---------- Gráficos ----------
    private static XRChart GraficoPartidas(ReportePresupuestoModelo m, PointF p, SizeF s)
    {
        var top = m.PorPartida.Take(12).ToList();
        var c = Grafico(m.PorPartida.Count > top.Count ? "Ejecución por partida (12 primeras)" : "Ejecución por partida", p, s, "Sin partidas en el año");
        if (top.Count == 0) return c;
        // Orden inverso: en barras horizontales el primer punto queda abajo
        var filas = Enumerable.Reverse(top).ToList();
        foreach (var (nombre, color, valor) in new (string, Color, Func<PartidaSerie, decimal>)[]
                 { ("Pagado", Marca, x => x.Pagado), ("Comprometido", Ambar, x => x.Comprometido), ("Disponible", Disponible, x => x.Disponible) })
        {
            var serie = Serie(nombre, ViewType.StackedBar, filas.Select(x => (Corto(x.Partida), (double)valor(x))));
            serie.LabelsVisibility = DefaultBoolean.False;
            var vista = (StackedBarSeriesView)serie.View;
            vista.Color = color; vista.FillStyle.FillMode = FillMode.Solid; vista.BarWidth = 0.6; vista.Border.Visibility = DefaultBoolean.False;
            c.Series.Add(serie);
        }
        EstiloEjes(c, true);
        if (c.Diagram is XYDiagram d) d.AxisY.Label.TextPattern = "{V:$ #,##0}";
        LeyendaAbajo(c);
        return c;
    }

    private static XRChart GraficoMeses(ReportePresupuestoModelo m, PointF p, SizeF s)
    {
        var c = Grafico("Pagos por mes", p, s, "Sin pagos en el año");
        if (m.PorMes.All(x => x.Pagado == 0 && x.Programado == 0)) return c;
        foreach (var (nombre, color, valor) in new (string, Color, Func<MesSerie, decimal>)[] { ("Pagado", Marca, x => x.Pagado), ("Por pagar", Ambar, x => x.Programado) })
        {
            var serie = Serie(nombre, ViewType.StackedBar, m.PorMes.Select(x => (x.Mes, (double)valor(x))));
            serie.LabelsVisibility = DefaultBoolean.False;
            var vista = (StackedBarSeriesView)serie.View;
            vista.Color = color; vista.FillStyle.FillMode = FillMode.Solid; vista.BarWidth = 0.6; vista.Border.Visibility = DefaultBoolean.False;
            c.Series.Add(serie);
        }
        EstiloEjes(c, false);
        if (c.Diagram is XYDiagram d)
        {
            d.AxisY.Label.TextPattern = "{V:$ #,##0}";
            // Los 12 meses rotulados, aunque no tengan pagos
            d.AxisX.QualitativeScaleOptions.AutoGrid = false;
            d.AxisX.QualitativeScaleOptions.GridSpacing = 1;
        }
        LeyendaAbajo(c);
        return c;
    }

    private static string Corto(string s) => s.Length <= 38 ? s : s[..37] + "…";

    // Filas del detalle con los montos y fechas ya formateados en español
    private sealed record FilaPartida(string Codigo, string Rubro, string Estado, string Asignado, string Ajustes, string Presupuestado,
        string Comprometido, string Pagado, string Disponible, string Ejecucion);
    private sealed record FilaServicio(string Codigo, string Servicio, string Proveedor, string Vence, string Partida, string TipoPresupuesto,
        string Estimado, string Pagado, string Diferencia, string Estado);
    private sealed record FilaPago(string Codigo, string Fecha, string Solicitud, string Descripcion, string Proveedor, string Partidas,
        string TipoPresupuesto, string Monto, string Estado);
}
