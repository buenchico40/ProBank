using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ProBank.Application.Modules.ConsultasDinamicas;

/// <summary>SQL-05: exportadores livianos. Excel y PDF se implementan en Infrastructure (requieren librerías).</summary>
public sealed class ExportadorCsv : IExportador
{
    public string Formato => "csv";
    public string TipoContenido => "text/csv";
    public byte[] Exportar(ResultadoEjecucion r)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', r.Columnas.Select(Escapar)));
        foreach (var fila in r.Filas)
            sb.AppendLine(string.Join(';', r.Columnas.Select(c => Escapar(Texto(fila.GetValueOrDefault(c))))));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
    internal static string Texto(object? v) => v switch
    {
        null => string.Empty,
        DateTime d => d.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        decimal m => m.ToString("0.00", CultureInfo.InvariantCulture),
        bool b => b ? "Sí" : "No",
        _ => v.ToString() ?? string.Empty
    };
    private static string Escapar(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
}

public sealed class ExportadorTexto : IExportador
{
    public string Formato => "txt";
    public string TipoContenido => "text/plain";
    public byte[] Exportar(ResultadoEjecucion r) => Encoding.UTF8.GetBytes(string.Join("\r\n",
        new[] { string.Join('\t', r.Columnas) }.Concat(r.Filas.Select(f => string.Join('\t', r.Columnas.Select(c => ExportadorCsv.Texto(f.GetValueOrDefault(c))))))));
}

public sealed class ExportadorJson : IExportador
{
    public string Formato => "json";
    public string TipoContenido => "application/json";
    public byte[] Exportar(ResultadoEjecucion r) => JsonSerializer.SerializeToUtf8Bytes(r.Filas, new JsonSerializerOptions { WriteIndented = true });
}
