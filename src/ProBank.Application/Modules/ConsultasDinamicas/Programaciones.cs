namespace ProBank.Application.Modules.ConsultasDinamicas;

public enum Frecuencia { Diaria, Semanal, Mensual }

public sealed record Programacion(int ProgramacionId, string ConsultaCodigo, int ConexionId, Frecuencia Frecuencia, TimeOnly Hora,
    IReadOnlyList<DayOfWeek> Dias, int? DiaDelMes, string Formato, IReadOnlyList<string> Destinatarios,
    IReadOnlyDictionary<string, string?> Parametros, int ResponsableId);

public interface IProgramacionRepository
{
    Task<IReadOnlyList<Programacion>> TomarVencidas(DateTime ahora, CancellationToken ct);
    Task MarcarEjecutada(int programacionId, DateTime proxima, string resultado, CancellationToken ct);
}

public static class CalendarioProgramacion
{
    /// <summary>SQL-07: próxima ejecución después de <paramref name="desde"/> (hora local de Ecuador).</summary>
    public static DateTime Proxima(Programacion p, DateTime desde)
    {
        var dia = DateOnly.FromDateTime(desde);
        for (var i = 0; i < 400; i++)
        {
            var d = dia.AddDays(i);
            var candidato = d.ToDateTime(p.Hora);
            if (candidato <= desde) continue;
            var ok = p.Frecuencia switch
            {
                Frecuencia.Diaria => true,
                Frecuencia.Semanal => p.Dias.Contains(d.DayOfWeek),
                Frecuencia.Mensual => p.DiaDelMes is null or 0
                    ? d.Day == DateTime.DaysInMonth(d.Year, d.Month)
                    : d.Day == Math.Min(p.DiaDelMes.Value, DateTime.DaysInMonth(d.Year, d.Month)),
                _ => false
            };
            if (ok) return candidato;
        }
        throw new InvalidOperationException("No se encontró una próxima ejecución.");
    }
}
