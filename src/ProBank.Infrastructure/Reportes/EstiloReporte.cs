using System.Drawing;
using System.Globalization;
using DevExpress.Drawing;
using DevExpress.Drawing.Printing;
using DevExpress.Utils;
using DevExpress.XtraCharts;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;

namespace ProBank.Infrastructure.Reportes;

/// <summary>
/// Diseño común de los reportes PDF (DevExpress XtraReports): A4 horizontal con la paleta de la aplicación, encabezado con
/// tarjetas de indicadores, encabezado y pie de página, secciones con gráficos y tablas con filas alternadas y estados en color.
/// </summary>
internal static class EstiloReporte
{
    // Paleta de la aplicación (wwwroot/css/site.css: --brand) y neutros
    public static readonly Color Marca = ColorTranslator.FromHtml("#D40E14");
    public static readonly Color Tinta = ColorTranslator.FromHtml("#1F2937");
    public static readonly Color Gris = ColorTranslator.FromHtml("#6B7280");
    public static readonly Color Linea = ColorTranslator.FromHtml("#E5E7EB");
    public static readonly Color Suave = ColorTranslator.FromHtml("#F8FAFC");
    public static readonly Color Pizarra = ColorTranslator.FromHtml("#334155");
    public static readonly Color Verde = ColorTranslator.FromHtml("#16A34A");
    public static readonly Color Ambar = ColorTranslator.FromHtml("#D97706");
    public static readonly Color Rojo = ColorTranslator.FromHtml("#DC2626");
    public static readonly Color Azul = ColorTranslator.FromHtml("#2563EB");

    private const string Fuente = "Segoe UI";
    public const float Ancho = 1089f; // A4 horizontal (1169) menos márgenes de 40, en centésimas de pulgada
    public static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-EC");

    /// <summary>Campos numéricos: se alinean a la derecha en las tablas.</summary>
    private static readonly HashSet<string> Derecha =
    [
        "TareasConMovimiento", "Acuerdos", "Cumplidos", "Vencidos",
        "Asignado", "Ajustes", "Presupuestado", "Comprometido", "Pagado", "Disponible", "Ejecucion", "Estimado", "Diferencia", "Monto",
    ];

    public static DXFont F(float tamano, DXFontStyle estilo = DXFontStyle.Regular) => new(Fuente, tamano, estilo);

    public static XtraReport Nuevo(string nombre)
    {
        var r = new XtraReport
        {
            PaperKind = DXPaperKind.A4, Landscape = true, Margins = new DXMargins(40, 40, 40, 40),
            Font = F(8.5f), ForeColor = Tinta, DisplayName = nombre,
        };
        r.StyleSheet.Add(new XRControlStyle { Name = "Par", BackColor = Suave });
        r.Bands.Add(new TopMarginBand { HeightF = 40 });
        r.Bands.Add(new BottomMarginBand { HeightF = 40 });
        return r;
    }

    public static byte[] Pdf(XtraReport reporte, string titulo)
    {
        // Los ejes de los gráficos formatean con la cultura del hilo: en español ($ 20.000, no $ 20,000)
        var cultura = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = Es;
        try
        {
            using var ms = new MemoryStream();
            reporte.ExportToPdf(ms, new PdfExportOptions { DocumentOptions = { Title = titulo, Author = "ProBank" } });
            return ms.ToArray();
        }
        finally { CultureInfo.CurrentCulture = cultura; }
    }

    // ---------- Encabezado, pie y tarjetas ----------
    public static ReportHeaderBand Encabezado(string titulo, string subtitulo, string codigo, IReadOnlyList<(string Titulo, string Valor, string Nota)> indicadores)
    {
        var b = new ReportHeaderBand { HeightF = 186 };
        b.Controls.Add(new XRPanel { LocationF = new PointF(0, 0), SizeF = new SizeF(Ancho, 5), BackColor = Marca, Borders = BorderSide.None });
        b.Controls.Add(Etiqueta("ProBank", new PointF(0, 16), new SizeF(200, 30), F(18, DXFontStyle.Bold), Marca));
        b.Controls.Add(Etiqueta(titulo, new PointF(0, 48), new SizeF(760, 30), F(16, DXFontStyle.Bold), Tinta));
        b.Controls.Add(Etiqueta(subtitulo, new PointF(0, 80), new SizeF(860, 18), F(9), Gris));
        var c = Etiqueta(codigo, new PointF(Ancho - 130, 20), new SizeF(130, 28), F(11, DXFontStyle.Bold), Marca);
        c.TextAlignment = TextAlignment.MiddleCenter;
        c.BackColor = ColorTranslator.FromHtml("#FDECEC");
        b.Controls.Add(c);

        Color[] acentos = [Marca, Azul, Pizarra, Verde, Ambar];
        const float y = 108, sep = 16, alto = 72;
        var ancho = (Ancho - sep * (indicadores.Count - 1)) / indicadores.Count;
        for (var i = 0; i < indicadores.Count; i++)
            b.Controls.Add(Tarjeta(indicadores[i], new PointF(i * (ancho + sep), y), new SizeF(ancho, alto), acentos[i % acentos.Length]));
        return b;
    }

    private static XRPanel Tarjeta((string Titulo, string Valor, string Nota) k, PointF p, SizeF s, Color acento)
    {
        var panel = new XRPanel { LocationF = p, SizeF = s, BackColor = Suave, BorderColor = Linea, Borders = BorderSide.All, BorderWidth = 1 };
        panel.Controls.Add(new XRPanel { LocationF = new PointF(0, 0), SizeF = new SizeF(4, s.Height), BackColor = acento, Borders = BorderSide.None });
        panel.Controls.Add(Etiqueta(k.Titulo.ToUpper(Es), new PointF(16, 8), new SizeF(s.Width - 24, 16), F(7.5f, DXFontStyle.Bold), Gris));
        panel.Controls.Add(Etiqueta(k.Valor, new PointF(16, 22), new SizeF(s.Width - 24, 28), F(s.Width < 220 ? 15 : 18, DXFontStyle.Bold), Tinta));
        panel.Controls.Add(Etiqueta(k.Nota, new PointF(16, 50), new SizeF(s.Width - 24, 16), F(8), Gris));
        return panel;
    }

    public static PageHeaderBand EncabezadoPagina(string izquierda, string derecha)
    {
        var b = new PageHeaderBand { HeightF = 28, PrintOn = PrintOnPages.NotWithReportHeader };
        b.Controls.Add(Etiqueta(izquierda, new PointF(0, 0), new SizeF(700, 18), F(8, DXFontStyle.Bold), Gris));
        b.Controls.Add(Etiqueta(derecha, new PointF(Ancho - 300, 0), new SizeF(300, 18), F(8), Gris, TextAlignment.TopRight));
        b.Controls.Add(new XRLine { LocationF = new PointF(0, 20), SizeF = new SizeF(Ancho, 4), ForeColor = Linea, LineWidth = 1 });
        return b;
    }

    public static PageFooterBand PiePagina(string texto)
    {
        var b = new PageFooterBand { HeightF = 26 };
        b.Controls.Add(new XRLine { LocationF = new PointF(0, 0), SizeF = new SizeF(Ancho, 4), ForeColor = Linea, LineWidth = 1 });
        b.Controls.Add(Etiqueta(texto, new PointF(0, 8), new SizeF(700, 16), F(7.5f), Gris));
        b.Controls.Add(new XRPageInfo
        {
            LocationF = new PointF(Ancho - 200, 8), SizeF = new SizeF(200, 16), PageInfo = PageInfo.NumberOfTotal, TextFormatString = "Página {0} de {1}",
            Font = F(7.5f), ForeColor = Gris, TextAlignment = TextAlignment.TopRight, Padding = new PaddingInfo(0, 0, 0, 0),
        });
        return b;
    }

    public static ReportFooterBand Nota(string texto) => new()
    {
        HeightF = 40,
        Controls = { Etiqueta(texto, new PointF(0, 14), new SizeF(Ancho, 20), F(7.5f, DXFontStyle.Italic), Gris) },
    };

    // ---------- Secciones ----------
    /// <summary>Sección sin datos (se imprime una vez): título opcional y contenido fijo, por ejemplo gráficos.</summary>
    public static DetailReportBand Seccion(int nivel, string? titulo, string? subtitulo, Action<DetailBand> contenido, float alto)
    {
        var s = new DetailReportBand { Level = nivel };
        if (titulo is not null) s.Bands.Add(new ReportHeaderBand { HeightF = 38, Controls = { TituloSeccion(titulo, subtitulo) } });
        var d = new DetailBand { HeightF = alto, KeepTogether = true };
        contenido(d);
        s.Bands.Add(d);
        return s;
    }

    private static XRLabel TituloSeccion(string titulo, string? subtitulo)
    {
        var l = Etiqueta(titulo + (subtitulo is null ? "" : "   " + subtitulo), new PointF(0, 12), new SizeF(Ancho, 22), F(11, DXFontStyle.Bold), Tinta);
        l.Borders = BorderSide.Bottom; l.BorderColor = Marca; l.BorderWidth = 2;
        return l;
    }

    /// <summary>
    /// Tabla de detalle: encabezado que se repite en cada página, filas alternadas y estados en color. Con <paramref name="totales"/>
    /// (campo → texto) cierra con una fila de totales; la primera columna dice «Total».
    /// </summary>
    public static DetailReportBand Tabla<T>(int nivel, string titulo, string vacio, bool saltoPagina, IReadOnlyList<T> filas,
        (string Titulo, string Campo, float Ancho)[] columnas, IReadOnlyDictionary<string, string>? totales = null)
    {
        var s = new DetailReportBand { Level = nivel, DataSource = filas, PageBreak = saltoPagina ? PageBreak.BeforeBand : PageBreak.None };
        var cab = new ReportHeaderBand { HeightF = 44 };
        cab.Controls.Add(TituloSeccion(titulo, filas.Count > 0 ? $"({filas.Count})" : null));
        s.Bands.Add(cab);
        if (filas.Count == 0)
        {
            cab.HeightF = 64;
            cab.Controls.Add(Etiqueta(vacio, new PointF(0, 42), new SizeF(Ancho, 18), F(8.5f, DXFontStyle.Italic), Gris));
            s.Bands.Add(new DetailBand { HeightF = 0 });
            return s;
        }

        var encabezado = new GroupHeaderBand { HeightF = 26, RepeatEveryPage = true, GroupUnion = GroupUnion.WithFirstDetail };
        encabezado.Controls.Add(Fila(columnas, c => c.Titulo, Tipo.Encabezado));
        s.Bands.Add(encabezado);

        var detalle = new DetailBand { HeightF = 22 };
        var tabla = Fila(columnas, c => $"[{c.Campo}]", Tipo.Detalle);
        tabla.EvenStyleName = "Par";
        detalle.Controls.Add(tabla);
        s.Bands.Add(detalle);

        if (totales is not null)
        {
            // Pie de grupo unido a la última fila: el total no queda solo en otra página
            var pie = new GroupFooterBand { HeightF = 26, GroupUnion = GroupFooterUnion.WithLastDetail };
            pie.Controls.Add(Fila(columnas, c => c == columnas[0] ? "Total" : totales.GetValueOrDefault(c.Campo, ""), Tipo.Total));
            s.Bands.Add(pie);
        }
        return s;
    }

    private enum Tipo { Encabezado, Detalle, Total }

    private static XRTable Fila((string Titulo, string Campo, float Ancho)[] columnas, Func<(string Titulo, string Campo, float Ancho), string> texto, Tipo tipo)
    {
        var t = new XRTable
        {
            LocationF = new PointF(0, 0), SizeF = new SizeF(Ancho, tipo == Tipo.Detalle ? 22 : 26),
            Borders = tipo == Tipo.Total ? BorderSide.Top : BorderSide.Bottom, BorderColor = tipo == Tipo.Total ? Tinta : Linea, BorderWidth = tipo == Tipo.Total ? 1.5f : 1,
        };
        t.BeginInit();
        var fila = new XRTableRow();
        foreach (var c in columnas)
        {
            var celda = new XRTableCell
            {
                WidthF = c.Ancho, CanGrow = true, WordWrap = true, Padding = new PaddingInfo(6, 6, 4, 4),
                TextAlignment = Derecha.Contains(c.Campo) ? TextAlignment.MiddleRight : TextAlignment.MiddleLeft,
            };
            switch (tipo)
            {
                case Tipo.Encabezado:
                    celda.Text = c.Titulo;
                    celda.Font = F(8, DXFontStyle.Bold);
                    celda.ForeColor = Color.White;
                    celda.BackColor = Tinta;
                    break;
                case Tipo.Total:
                    celda.Text = texto(c);
                    celda.Font = F(8.5f, DXFontStyle.Bold);
                    break;
                default:
                    celda.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", texto(c)));
                    if (c.Campo == "Estado")
                    {
                        celda.Font = F(8.5f, DXFontStyle.Bold);
                        celda.BeforePrint += (s, _) => { var x = (XRTableCell)s!; x.ForeColor = ColorEstado(x.Report.GetCurrentColumnValue("Estado") as string); };
                    }
                    break;
            }
            fila.Cells.Add(celda);
        }
        t.Rows.Add(fila);
        t.EndInit();
        return t;
    }

    private static Color ColorEstado(string? estado) => estado switch
    {
        "Vencido" or "Anulado" or "Rechazada" => Rojo,
        "Cumplido" or "Completado" or "Pagado" or "Activo" or "Vigente" => Verde,
        "Pendiente" or "Borrador" or "Por vencer" or "Programado" => Ambar,
        "En curso" or "En revisión" or "Requerimiento enviado" => Azul,
        "Cerrado" or "Retirado" => Gris,
        _ => Tinta,
    };

    // ---------- Gráficos ----------
    public static XRChart Grafico(string titulo, PointF p, SizeF s, string vacio = "Sin datos en el periodo")
    {
        var c = new XRChart { LocationF = p, SizeF = s, BorderColor = Linea, Borders = BorderSide.All, BackColor = Color.White };
        c.Titles.Add(new ChartTitle { Text = titulo, Font = F(10, DXFontStyle.Bold), TextColor = Tinta, Alignment = StringAlignment.Near, Indent = 8 });
        c.Legend.Visibility = DefaultBoolean.False;
        c.EmptyChartText.Text = vacio;
        c.EmptyChartText.Font = F(9, DXFontStyle.Italic);
        c.EmptyChartText.TextColor = Gris;
        return c;
    }

    public static void EstiloEjes(XRChart c, bool rotado)
    {
        if (c.Diagram is not XYDiagram d) return;
        d.Rotated = rotado;
        d.DefaultPane.BorderVisible = false;
        d.AxisX.Color = Linea; d.AxisY.Color = Linea;
        d.AxisX.Label.Font = F(7.5f); d.AxisX.Label.TextColor = Gris;
        d.AxisY.Label.Font = F(7.5f); d.AxisY.Label.TextColor = Gris;
        d.AxisY.GridLines.Color = Linea;
        d.AxisX.Tickmarks.Visible = false; d.AxisY.Tickmarks.Visible = false;
        // En barras horizontales cada categoría tiene su rótulo (sin ocultar los que DevExpress considera apretados)
        if (rotado) d.AxisX.Label.ResolveOverlappingOptions.AllowHide = false;
    }

    public static Series Serie(string nombre, ViewType tipo, IEnumerable<(string Argumento, double Valor)> puntos)
    {
        var s = new Series(nombre, tipo) { LabelsVisibility = DefaultBoolean.True };
        foreach (var (a, v) in puntos) s.Points.Add(new SeriesPoint(a, v));
        s.Label.Font = F(7.5f);
        s.Label.TextColor = Tinta;
        return s;
    }

    /// <summary>Valor al final de la barra, en tinta sobre fondo blanco (contraste legible).</summary>
    public static void EtiquetaFueraDeBarra(Series serie)
    {
        ((BarSeriesLabel)serie.Label).Position = BarSeriesLabelPosition.Top;
        serie.Label.Border.Visibility = DefaultBoolean.False;
        serie.Label.BackColor = Color.Transparent;
        serie.Label.Font = F(7.5f, DXFontStyle.Bold);
        serie.Label.TextColor = Tinta;
        serie.Label.LineVisibility = DefaultBoolean.False;
    }

    public static void LeyendaAbajo(XRChart c)
    {
        c.Legend.Visibility = DefaultBoolean.True;
        c.Legend.AlignmentHorizontal = LegendAlignmentHorizontal.Center;
        c.Legend.AlignmentVertical = LegendAlignmentVertical.BottomOutside;
        c.Legend.Direction = LegendDirection.LeftToRight;
        c.Legend.Border.Visibility = DefaultBoolean.False;
        c.Legend.Font = F(8);
        c.Legend.TextColor = Tinta;
    }

    public static XRLabel Etiqueta(string texto, PointF p, SizeF s, DXFont fuente, Color color, TextAlignment alineacion = TextAlignment.TopLeft) =>
        new() { Text = texto, LocationF = p, SizeF = s, Font = fuente, ForeColor = color, TextAlignment = alineacion, Padding = new PaddingInfo(0, 0, 0, 0), CanGrow = true, WordWrap = true, Borders = BorderSide.None };
}
