using System.Text.RegularExpressions;
using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Coactivas;

public enum TipoOrden { Bloqueo, Levantamiento, Transferencia }
public enum Cobertura { SinBloqueo, Parcial, Total }

public static class CoberturaCoactiva
{
    /// <summary>COA-05: cobertura según lo bloqueado vigente más lo ya transferido frente al monto de la coactiva.</summary>
    public static Cobertura Calcular(decimal monto, decimal bloqueadoVigente, decimal transferido = 0m) =>
        bloqueadoVigente + transferido <= 0m ? Cobertura.SinBloqueo : bloqueadoVigente + transferido >= monto ? Cobertura.Total : Cobertura.Parcial;
}

/// <summary>Reglas de datos de Coactivas que no dependen de la base (la base valida unicidad y saldos).</summary>
public static partial class ReglasCoactiva
{
    public static readonly IReadOnlyList<string> TiposProducto = ["Cuenta de ahorros", "Cuenta corriente", "Depósito a plazo fijo", "Inversión"];
    public static readonly IReadOnlyList<string> TiposIdentificacion = ["Cédula", "RUC", "Pasaporte"];

    /// <summary>Nombres de la orden en el Excel y en las pantallas.</summary>
    public static readonly IReadOnlyDictionary<string, TipoOrden> Ordenes = new Dictionary<string, TipoOrden>(StringComparer.OrdinalIgnoreCase)
    {
        ["Bloqueo de fondos"] = TipoOrden.Bloqueo, ["Levantamiento de bloqueo"] = TipoOrden.Levantamiento, ["Transferencia de fondos"] = TipoOrden.Transferencia,
    };

    [GeneratedRegex(@"^\d{10}$")] private static partial Regex Cedula();
    [GeneratedRegex(@"^\d{13}$")] private static partial Regex Ruc13();
    [GeneratedRegex(@"^[A-Za-z0-9]{1,20}$")] private static partial Regex Pasaporte();

    /// <summary>Cédula: 10 dígitos · RUC: 13 dígitos · Pasaporte: hasta 20 caracteres alfanuméricos.</summary>
    public static string? ValidarIdentificacion(string tipo, string identificacion) => tipo switch
    {
        "Cédula" => Cedula().IsMatch(identificacion) ? null : "La cédula debe tener 10 dígitos.",
        "RUC" => Ruc13().IsMatch(identificacion) ? null : "El RUC debe tener 13 dígitos.",
        "Pasaporte" => Pasaporte().IsMatch(identificacion) ? null : "El pasaporte admite hasta 20 letras o números.",
        _ => "El tipo de identificación debe ser Cédula, RUC o Pasaporte.",
    };

    public static string? ValidarRuc(string ruc) => Ruc13().IsMatch(ruc) ? null : "El RUC de la entidad debe tener 13 dígitos.";

    /// <summary>Montos en dólares, mayores que 0 y con hasta 2 decimales.</summary>
    public static string? ValidarMonto(decimal? monto, string nombre) =>
        monto is null or <= 0 ? $"{nombre} debe ser mayor que 0."
        : decimal.Round(monto.Value, 2) != monto.Value ? $"{nombre} admite hasta 2 decimales."
        : null;

    /// <summary>COA-03: tipos de producto separados por coma (vacío = todos). Devuelve la lista normalizada o el error.</summary>
    public static (string? Productos, string? Error) NormalizarProductos(string? productos)
    {
        if (string.IsNullOrWhiteSpace(productos)) return (null, null);
        var lista = productos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => TiposProducto.FirstOrDefault(t => string.Equals(t, p, StringComparison.OrdinalIgnoreCase)) ?? p).Distinct().ToList();
        var malos = lista.Where(p => !TiposProducto.Contains(p)).ToList();
        return malos.Count > 0 ? (null, $"Tipo de producto no válido: {string.Join(", ", malos)}.") : (string.Join(",", lista), null);
    }
}

/// <summary>Una fila de la hoja «Coactivas» del formato de carga masiva (spec/formatos), ya leída como texto.</summary>
public sealed record FilaCarga(int Fila, string Oficio, string Recibido, string RemitidoPor, string Orden, string Proceso, string Ruc,
    string TipoIdentificacion, string Identificacion, string Cliente, string Monto, string Productos, string MontoOrdenado, string Observacion);

/// <summary>Fila válida en formato, lista para que la base valide lo que depende de sus datos.</summary>
public sealed record FilaValida(int Fila, string Oficio, DateOnly Recibido, string RemitidoPor, TipoOrden Tipo, string Proceso, string Ruc,
    string TipoIdentificacion, string Identificacion, string Cliente, decimal? Monto, string? Productos, decimal? MontoOrdenado, string? Observacion);

public sealed record ErrorCarga(int Fila, string Oficio, string Motivo);

/// <summary>
/// COA-04: reglas de formato del Excel (hoja Instrucciones). Si una fila tiene errores, su oficio completo se descarta;
/// se informan todas las filas con su motivo.
/// </summary>
public static class CargaMasiva
{
    public static (IReadOnlyList<FilaValida> Validas, IReadOnlyList<ErrorCarga> Errores) Validar(IReadOnlyList<FilaCarga> filas, DateOnly hoy)
    {
        var errores = new List<ErrorCarga>();
        var validas = new List<FilaValida>();
        foreach (var f in filas)
        {
            var motivos = new List<string>();
            void Req(string valor, string nombre, int max) { if (string.IsNullOrWhiteSpace(valor)) motivos.Add($"{nombre} es obligatorio."); else if (valor.Trim().Length > max) motivos.Add($"{nombre} admite hasta {max} caracteres."); }
            Req(f.Oficio, "N.º de oficio", 30); Req(f.RemitidoPor, "Remitido por", 150); Req(f.Proceso, "N.º de proceso", 50);
            Req(f.Cliente, "Cliente / empresa", 150);

            DateOnly recibido = default;
            if (!DateOnly.TryParseExact(f.Recibido.Trim(), ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"], out recibido)) motivos.Add("Fecha de recepción no válida (dd/mm/aaaa).");
            else if (recibido > hoy) motivos.Add("La fecha de recepción no puede ser posterior a la fecha de carga.");

            var tipo = ReglasCoactiva.Ordenes.TryGetValue(f.Orden.Trim(), out var t) ? t : (TipoOrden?)null;
            if (tipo is null) motivos.Add("Orden no válida: Bloqueo de fondos, Levantamiento de bloqueo o Transferencia de fondos.");
            if (ReglasCoactiva.ValidarRuc(f.Ruc.Trim()) is { } er) motivos.Add(er);
            if (ReglasCoactiva.ValidarIdentificacion(f.TipoIdentificacion.Trim(), f.Identificacion.Trim()) is { } ei) motivos.Add(ei);

            decimal? monto = null, ordenado = null;
            string? productos = null;
            if (tipo == TipoOrden.Bloqueo)
            {
                monto = Numero(f.Monto);
                if (ReglasCoactiva.ValidarMonto(monto, "El monto de la coactiva") is { } em) motivos.Add(em);
                var (p, ep) = ReglasCoactiva.NormalizarProductos(f.Productos);
                if (ep is not null) motivos.Add(ep); else productos = p;
            }
            else if (tipo is not null)
            {
                if (!string.IsNullOrWhiteSpace(f.Productos)) motivos.Add("Productos a bloquear solo aplica a la orden de bloqueo.");
                if (!string.IsNullOrWhiteSpace(f.MontoOrdenado))
                {
                    ordenado = Numero(f.MontoOrdenado);
                    if (ReglasCoactiva.ValidarMonto(ordenado, "El monto ordenado") is { } eo) motivos.Add(eo);
                }
            }

            if (motivos.Count > 0) errores.AddRange(motivos.Select(m => new ErrorCarga(f.Fila, f.Oficio.Trim(), m)));
            else validas.Add(new FilaValida(f.Fila, f.Oficio.Trim(), recibido, f.RemitidoPor.Trim(), tipo!.Value, f.Proceso.Trim(), f.Ruc.Trim(),
                f.TipoIdentificacion.Trim(), f.Identificacion.Trim(), f.Cliente.Trim(), monto, productos, ordenado, NullSiVacio(f.Observacion)));
        }

        // Filas duplicadas (mismo oficio, orden y proceso) y datos del oficio distintos entre sus filas
        foreach (var g in validas.GroupBy(v => (v.Oficio, v.Tipo, v.Proceso)).Where(g => g.Count() > 1))
            errores.AddRange(g.Skip(1).Select(v => new ErrorCarga(v.Fila, v.Oficio, "Fila duplicada: mismo oficio, orden y N.º de proceso.")));
        foreach (var g in validas.GroupBy(v => v.Oficio).Where(g => g.Select(v => (v.Recibido, v.RemitidoPor)).Distinct().Count() > 1))
            errores.AddRange(g.Select(v => new ErrorCarga(v.Fila, v.Oficio, "La fecha de recepción y el remitente deben ser iguales en todas las filas del oficio.")));

        var conError = errores.Select(e => e.Oficio).ToHashSet();
        return (validas.Where(v => !conError.Contains(v.Oficio)).ToList(), errores.OrderBy(e => e.Fila).ToList());
    }

    // Acepta punto o coma decimal según la configuración regional con la que se llenó el Excel.
    private static decimal? Numero(string texto)
    {
        var t = texto.Trim().Replace("$", "").Replace(" ", "");
        if (t.Length == 0) return null;
        if (t.Contains(',') && t.Contains('.')) t = t.LastIndexOf(',') > t.LastIndexOf('.') ? t.Replace(".", "").Replace(',', '.') : t.Replace(",", "");
        else t = t.Replace(',', '.');
        return decimal.TryParse(t, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static string? NullSiVacio(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
