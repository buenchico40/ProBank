namespace ProBank.Domain.Modules.ConsultasDinamicas;

/// <summary>SQL-04: valores variables que se resuelven en el momento de cada ejecución.</summary>
public static class VariablesParametro
{
    public static readonly IReadOnlyDictionary<string, Func<DateOnly, DateOnly>> Fechas = new Dictionary<string, Func<DateOnly, DateOnly>>
    {
        ["Hoy"] = d => d,
        ["Ayer"] = d => d.AddDays(-1),
        ["Inicio de semana"] = d => d.AddDays(-(((int)d.DayOfWeek + 6) % 7)),
        ["Fin de semana"] = d => d.AddDays(-(((int)d.DayOfWeek + 6) % 7) + 6),
        ["Inicio de mes"] = d => new DateOnly(d.Year, d.Month, 1),
        ["Fin de mes"] = d => new DateOnly(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month)),
        ["Inicio del mes anterior"] = d => new DateOnly(d.Year, d.Month, 1).AddMonths(-1),
        ["Fin del mes anterior"] = d => new DateOnly(d.Year, d.Month, 1).AddDays(-1),
        ["Inicio de trimestre"] = d => new DateOnly(d.Year, ((d.Month - 1) / 3 * 3) + 1, 1),
        ["Fin de trimestre"] = d => new DateOnly(d.Year, ((d.Month - 1) / 3 * 3) + 1, 1).AddMonths(3).AddDays(-1),
        ["Inicio de año"] = d => new DateOnly(d.Year, 1, 1),
        ["Fin de año"] = d => new DateOnly(d.Year, 12, 31),
    };

    public static readonly IReadOnlyDictionary<string, Func<DateOnly, int>> Numeros = new Dictionary<string, Func<DateOnly, int>>
    {
        ["Año actual"] = d => d.Year,
        ["Año anterior"] = d => d.Year - 1,
        ["Mes actual"] = d => d.Month,
    };

    public static bool EsVariable(string? valor) =>
        valor is not null && (Fechas.ContainsKey(valor) || Numeros.ContainsKey(valor));

    /// <summary>Devuelve el valor resuelto; si no es una variable, devuelve el valor tal cual.</summary>
    public static object? Resolver(string? valor, DateOnly hoy)
    {
        if (valor is null) return null;
        if (Fechas.TryGetValue(valor, out var f)) return f(hoy);
        if (Numeros.TryGetValue(valor, out var n)) return n(hoy);
        return valor;
    }
}
