using System.Data;
using System.Drawing;
using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using ProBank.Application.Modules.ConsultasDinamicas;
using static ProBank.Infrastructure.Reportes.EstiloReporte;

namespace ProBank.Infrastructure.Reportes;

/// <summary>SQL-05: resultado de una consulta dinámica en Excel (.xlsx), con los números y fechas como valores, no como texto.</summary>
internal sealed class ExportadorXlsx : IExportador
{
    public string Formato => "xlsx";
    public string TipoContenido => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Exportar(ResultadoEjecucion resultado, string titulo)
    {
        using var r = ResultadoReporte.Crear(resultado, titulo, conEncabezado: false);
        using var ms = new MemoryStream();
        r.ExportToXlsx(ms, new XlsxExportOptions { ExportMode = XlsxExportMode.SingleFile, TextExportMode = TextExportMode.Value, SheetName = "Resultado", ShowGridLines = true });
        return ms.ToArray();
    }
}

/// <summary>SQL-05: resultado de una consulta dinámica en PDF (A4 horizontal con el diseño de los reportes de ProBank).</summary>
internal sealed class ExportadorPdf : IExportador
{
    public string Formato => "pdf";
    public string TipoContenido => "application/pdf";

    public byte[] Exportar(ResultadoEjecucion resultado, string titulo)
    {
        using var r = ResultadoReporte.Crear(resultado, titulo, conEncabezado: true);
        return EstiloReporte.Pdf(r, titulo);
    }
}

internal static class ResultadoReporte
{
    public static XtraReport Crear(ResultadoEjecucion res, string titulo, bool conEncabezado)
    {
        var r = Nuevo(titulo);
        r.DataSource = Tabla(res);
        var n = res.Columnas.Count;
        var fuente = n > 12 ? 6.5f : n > 8 ? 7.5f : 8.5f;
        var ancho = Ancho / Math.Max(1, n);

        if (conEncabezado)
        {
            var cab = new ReportHeaderBand { HeightF = 74 };
            cab.Controls.Add(new XRPanel { LocationF = new PointF(0, 0), SizeF = new SizeF(Ancho, 5), BackColor = Marca, Borders = BorderSide.None });
            cab.Controls.Add(Etiqueta(titulo, new PointF(0, 14), new SizeF(Ancho, 26), F(14, DXFontStyle.Bold), Tinta));
            cab.Controls.Add(Etiqueta($"{res.Filas.Count} filas{(res.Truncado ? " (truncado al límite de la consulta)" : "")} · generado el {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Es)}",
                new PointF(0, 44), new SizeF(Ancho, 18), F(8.5f), Gris));
            r.Bands.Add(cab);
            r.Bands.Add(PiePagina("ProBank · " + titulo));
        }

        var encabezado = new PageHeaderBand { HeightF = 26 };
        encabezado.Controls.Add(Fila(res, ancho, fuente, esEncabezado: true));
        r.Bands.Add(encabezado);
        var detalle = new DetailBand { HeightF = 20 };
        var t = Fila(res, ancho, fuente, esEncabezado: false);
        t.EvenStyleName = "Par";
        detalle.Controls.Add(t);
        r.Bands.Add(detalle);
        return r;
    }

    private static XRTable Fila(ResultadoEjecucion res, float ancho, float fuente, bool esEncabezado)
    {
        var t = new XRTable { LocationF = new PointF(0, 0), SizeF = new SizeF(Ancho, esEncabezado ? 26 : 20), Borders = BorderSide.Bottom, BorderColor = Linea };
        t.BeginInit();
        var fila = new XRTableRow();
        for (var i = 0; i < res.Columnas.Count; i++)
        {
            var tipo = res.Tipos[i];
            var celda = new XRTableCell
            {
                WidthF = ancho, CanGrow = true, WordWrap = true, Padding = new PaddingInfo(4, 4, 3, 3), Font = F(fuente),
                TextAlignment = tipo is "numero" or "entero" ? DevExpress.XtraPrinting.TextAlignment.MiddleRight : DevExpress.XtraPrinting.TextAlignment.MiddleLeft,
            };
            if (esEncabezado)
            {
                celda.Text = res.Columnas[i];
                celda.Font = F(fuente, DXFontStyle.Bold);
                celda.ForeColor = Color.White;
                celda.BackColor = Tinta;
            }
            else
            {
                celda.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", $"[{Col(i)}]"));
                celda.TextFormatString = tipo switch { "numero" => "{0:#,##0.00}", "entero" => "{0:#,##0}", "fecha" => "{0:dd/MM/yyyy}", "fechahora" => "{0:dd/MM/yyyy HH:mm}", _ => "" };
            }
            fila.Cells.Add(celda);
        }
        t.Rows.Add(fila);
        t.EndInit();
        return t;
    }

    // Columnas internas C0, C1… (los nombres del resultado pueden tener espacios o símbolos)
    private static string Col(int i) => "C" + i;

    private static DataTable Tabla(ResultadoEjecucion res)
    {
        var dt = new DataTable("Resultado");
        for (var i = 0; i < res.Columnas.Count; i++)
            dt.Columns.Add(Col(i), res.Tipos[i] switch
            {
                "numero" => typeof(decimal), "entero" => typeof(long), "fecha" or "fechahora" => typeof(DateTime), "bool" => typeof(string), _ => typeof(string),
            });
        foreach (var f in res.Filas)
            dt.Rows.Add(res.Columnas.Select((c, i) => f.GetValueOrDefault(c) switch
            {
                null => DBNull.Value,
                bool b => b ? "Sí" : "No",
                var v when res.Tipos[i] == "numero" => Convert.ToDecimal(v, Es),
                var v when res.Tipos[i] == "entero" => Convert.ToInt64(v, Es),
                var v when res.Tipos[i] is "fecha" or "fechahora" => v is DateTime d ? d : DateTime.Parse(v.ToString()!, Es),
                var v when res.Tipos[i] is "texto" => v.ToString()!,
                var v => v,
            }).ToArray());
        return dt;
    }
}
