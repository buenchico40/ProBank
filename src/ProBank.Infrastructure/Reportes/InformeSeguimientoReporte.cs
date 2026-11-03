using System.Drawing;
using DevExpress.Drawing;
using DevExpress.Utils;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.UI;
using ProBank.Application.Modules.Portafolio;
using static ProBank.Infrastructure.Reportes.EstiloReporte;

namespace ProBank.Infrastructure.Reportes;

/// <summary>
/// Informe de seguimiento con DevExpress Reporting (XtraReports): A4 horizontal; primero los indicadores y los gráficos
/// del periodo y luego el detalle (resumen por ítem, bitácora de tareas, acuerdos y temas). Se exporta a PDF.
/// </summary>
internal sealed class GeneradorInformeDevExpress : IGeneradorInforme
{
    public byte[] Pdf(InformeModelo modelo)
    {
        using var reporte = InformeSeguimientoReporte.Crear(modelo);
        return EstiloReporte.Pdf(reporte, $"{modelo.Codigo} · {modelo.Titulo}");
    }
}

internal static class InformeSeguimientoReporte
{
    public static XtraReport Crear(InformeModelo m)
    {
        var r = Nuevo(m.Codigo);
        r.Bands.Add(Encabezado(m.Titulo, m.Periodo + "   ·   " + m.Generado, m.Codigo, m.Indicadores.Select(k => (k.Titulo, k.Valor, k.Nota)).ToList()));
        r.Bands.Add(EncabezadoPagina($"ProBank · {m.Titulo} · {m.Codigo}", m.Periodo));
        r.Bands.Add(new DetailBand { HeightF = 0 });
        r.Bands.Add(PiePagina($"{m.Codigo} · {m.Generado}"));

        var n = 0;
        r.Bands.Add(Seccion(n++, "Panorama del periodo", null, b =>
        {
            b.Controls.Add(GraficoAvance(m, new PointF(0, 0), new SizeF(650, 236)));
            b.Controls.Add(GraficoAcuerdos(m, new PointF(666, 0), new SizeF(Ancho - 666, 236)));
        }, 244));
        r.Bands.Add(Seccion(n++, null, null, b =>
        {
            b.Controls.Add(GraficoMovimientos(m, new PointF(0, 8), new SizeF(650, 196)));
            b.Controls.Add(GraficoEventos(m, new PointF(666, 8), new SizeF(Ancho - 666, 196)));
        }, 206));

        r.Bands.Add(Tabla(n++, "Resumen por proyecto, mejora o bug", "Sin actividad en el periodo.", true,
            m.Resumen.Select(x => new FilaResumen(x.Item, x.Titulo, x.Tipo, x.Estado, $"{x.Avance} % ({x.Completadas} de {x.Tareas})",
                x.TareasConMovimiento, x.Acuerdos, x.Cumplidos, x.Vencidos)).ToList(),
            [("Ítem", "Item", 80), ("Título", "Titulo", 299), ("Tipo", "Tipo", 80), ("Estado", "Estado", 100), ("Avance", "Avance", 130),
             ("Tareas con movimiento", "TareasConMovimiento", 130), ("Acuerdos", "Acuerdos", 90), ("Cumplidos", "Cumplidos", 90), ("Vencidos", "Vencidos", 90)]));

        r.Bands.Add(Tabla(n++, "Bitácora de tareas", "Sin movimientos de tareas en el periodo.", false,
            m.Bitacora.Select(x => new FilaBitacora(x.Fecha.ToString("dd/MM/yyyy HH:mm", Es), x.Item, $"{x.Tarea} · {x.Titulo}", x.Evento, x.Detalle ?? "—", x.Usuario)).ToList(),
            [("Fecha", "Fecha", 115), ("Ítem", "Item", 80), ("Tarea", "Tarea", 270), ("Evento", "Evento", 150), ("Detalle", "Detalle", 334), ("Usuario", "Usuario", 140)]));

        r.Bands.Add(Tabla(n++, "Acuerdos semanales", "Sin acuerdos en el periodo.", false,
            m.Acuerdos.Select(x => new FilaAcuerdo(x.FechaRevision.ToString("dd/MM/yyyy", Es), x.Item, x.Codigo, x.Descripcion, x.Responsable,
                x.FechaCumplimiento.ToString("dd/MM/yyyy", Es), x.Estado)).ToList(),
            [("Revisión", "Revision", 90), ("Ítem", "Item", 80), ("Acuerdo", "Codigo", 90), ("Descripción", "Descripcion", 409), ("Responsable", "Responsable", 160),
             ("Cumplir hasta", "CumplirHasta", 110), ("Estado al cierre", "Estado", 150)]));

        if (m.Temas.Count > 0)
            r.Bands.Add(Tabla(n++, "Temas tratados en las revisiones", "", false,
                m.Temas.Select(x => new FilaTema(x.Fecha.ToString("dd/MM/yyyy", Es), x.Item, x.Temas)).ToList(),
                [("Fecha", "Fecha", 100), ("Ítem", "Item", 100), ("Temas", "Temas", Ancho - 200)]));

        r.Bands.Add(Nota("Avance del ítem = tareas completadas ÷ total de tareas. Los acuerdos pendientes con la fecha de cumplimiento pasada figuran como vencidos."));
        return r;
    }

    // ---------- Gráficos ----------
    private static XRChart GraficoAvance(InformeModelo m, PointF p, SizeF s)
    {
        var top = m.AvancePorItem.Take(12).ToList();
        var c = Grafico(m.AvancePorItem.Count > top.Count ? "Avance por ítem (12 con mayor avance)" : "Avance por ítem", p, s);
        if (top.Count == 0) return c;
        // Orden inverso: en barras horizontales el primer punto queda abajo
        var serie = Serie("Avance", ViewType.Bar, Enumerable.Reverse(top).Select(x => (x.Item, (double)x.Avance)));
        var vista = (BarSeriesView)serie.View;
        vista.Color = Marca; vista.FillStyle.FillMode = FillMode.Solid; vista.BarWidth = 0.6; vista.Border.Visibility = DefaultBoolean.False;
        serie.Label.TextPattern = "{V} %";
        EtiquetaFueraDeBarra(serie);
        c.Series.Add(serie);
        EstiloEjes(c, true);
        if (c.Diagram is XYDiagram d)
        {
            // 0 a 100 %, con espacio a la derecha para la etiqueta de las barras llenas
            d.AxisY.WholeRange.AutoSideMargins = false; d.AxisY.WholeRange.SideMarginsValue = 0;
            d.AxisY.WholeRange.SetMinMaxValues(0, 108); d.AxisY.VisualRange.SetMinMaxValues(0, 108);
            d.AxisY.NumericScaleOptions.AutoGrid = false; d.AxisY.NumericScaleOptions.GridSpacing = 20;
            d.AxisY.Label.TextPattern = "{V} %";
        }
        return c;
    }

    private static XRChart GraficoAcuerdos(InformeModelo m, PointF p, SizeF s)
    {
        var c = Grafico("Acuerdos por estado", p, s);
        if (m.AcuerdosPorEstado.Count == 0) return c;
        var serie = Serie("Acuerdos", ViewType.Doughnut, m.AcuerdosPorEstado.Select(x => (x.Nombre, (double)x.Cantidad)));
        foreach (SeriesPoint punto in serie.Points)
            punto.Color = punto.Argument switch { "Cumplido" => Verde, "Vencido" => Rojo, "Pendiente" => Ambar, _ => Pizarra };
        var vista = (DoughnutSeriesView)serie.View;
        vista.HoleRadiusPercent = 58;
        vista.FillStyle.FillMode = FillMode.Solid;
        vista.Border.Visibility = DefaultBoolean.False;
        serie.Label.TextPattern = "{V}";
        ((DoughnutSeriesLabel)serie.Label).Position = PieSeriesLabelPosition.Inside;
        serie.Label.TextColor = Color.White;
        serie.Label.Border.Visibility = DefaultBoolean.False;
        serie.Label.BackColor = Color.Transparent;
        serie.LegendTextPattern = "{A}: {V}";
        c.Series.Add(serie);
        LeyendaAbajo(c);
        return c;
    }

    private static XRChart GraficoMovimientos(InformeModelo m, PointF p, SizeF s)
    {
        var c = Grafico("Movimientos de tareas por día", p, s);
        if (m.MovimientosPorDia.All(x => x.Cantidad == 0)) return c;
        var serie = Serie("Movimientos", ViewType.Area, m.MovimientosPorDia.Select(x => (x.Nombre, (double)x.Cantidad)));
        var vista = (AreaSeriesView)serie.View;
        vista.Color = Color.FromArgb(55, Marca);
        vista.FillStyle.FillMode = FillMode.Solid;
        vista.Border.Visibility = DefaultBoolean.True; vista.Border.Color = Marca; vista.Border.Thickness = 2;
        vista.MarkerVisibility = DefaultBoolean.True;
        vista.MarkerOptions.Color = Marca; vista.MarkerOptions.Size = 5;
        // Solo se rotulan los días con movimientos (los ceros saturan el gráfico)
        serie.LabelsVisibility = DefaultBoolean.False;
        c.Series.Add(serie);
        var rotulos = Serie("Rótulos", ViewType.Point, m.MovimientosPorDia.Where(x => x.Cantidad > 0).Select(x => (x.Nombre, (double)x.Cantidad)));
        ((PointSeriesView)rotulos.View).Color = Marca;
        ((PointSeriesView)rotulos.View).PointMarkerOptions.Size = 7;
        rotulos.Label.Font = F(7.5f, DXFontStyle.Bold);
        rotulos.Label.Border.Visibility = DefaultBoolean.False;
        rotulos.Label.BackColor = Color.Transparent;
        rotulos.Label.LineVisibility = DefaultBoolean.False;
        c.Series.Add(rotulos);
        EstiloEjes(c, false);
        if (c.Diagram is XYDiagram d) d.AxisX.Label.ResolveOverlappingOptions.AllowRotate = true;
        return c;
    }

    private static XRChart GraficoEventos(InformeModelo m, PointF p, SizeF s)
    {
        var c = Grafico("Eventos de tareas por tipo", p, s);
        if (m.EventosPorTipo.Count == 0) return c;
        var serie = Serie("Eventos", ViewType.Bar, Enumerable.Reverse(m.EventosPorTipo.Take(8).ToList()).Select(x => (x.Nombre, (double)x.Cantidad)));
        var vista = (BarSeriesView)serie.View;
        vista.Color = Pizarra; vista.FillStyle.FillMode = FillMode.Solid; vista.BarWidth = 0.6; vista.Border.Visibility = DefaultBoolean.False;
        EtiquetaFueraDeBarra(serie);
        c.Series.Add(serie);
        EstiloEjes(c, true);
        return c;
    }

    // Filas del detalle con los textos ya formateados (fechas y avance en español)
    private sealed record FilaResumen(string Item, string Titulo, string Tipo, string Estado, string Avance, int TareasConMovimiento, int Acuerdos, int Cumplidos, int Vencidos);
    private sealed record FilaBitacora(string Fecha, string Item, string Tarea, string Evento, string Detalle, string Usuario);
    private sealed record FilaAcuerdo(string Revision, string Item, string Codigo, string Descripcion, string Responsable, string CumplirHasta, string Estado);
    private sealed record FilaTema(string Fecha, string Item, string Temas);
}
