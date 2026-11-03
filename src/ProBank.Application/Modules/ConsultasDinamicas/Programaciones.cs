using System.Globalization;
using ProBank.Application.Abstractions;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.Application.Modules.ConsultasDinamicas;

public enum Frecuencia { Diaria, Semanal, Mensual }

public sealed record Programacion(int ProgramacionId, string Codigo, string ConsultaCodigo, string ConexionCodigo, Frecuencia Frecuencia, TimeOnly Hora,
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
    public static DateTime Proxima(Programacion p, DateTime desde) => Proxima(p.Frecuencia, p.Hora, p.Dias, p.DiaDelMes, desde);

    public static DateTime Proxima(Frecuencia frecuencia, TimeOnly hora, IReadOnlyCollection<DayOfWeek> dias, int? diaDelMes, DateTime desde)
    {
        var dia = DateOnly.FromDateTime(desde);
        for (var i = 0; i < 400; i++)
        {
            var d = dia.AddDays(i);
            var candidato = d.ToDateTime(hora);
            if (candidato <= desde) continue;
            var ok = frecuencia switch
            {
                Frecuencia.Diaria => true,
                Frecuencia.Semanal => dias.Contains(d.DayOfWeek),
                Frecuencia.Mensual => diaDelMes is null or 0
                    ? d.Day == DateTime.DaysInMonth(d.Year, d.Month)
                    : d.Day == Math.Min(diaDelMes.Value, DateTime.DaysInMonth(d.Year, d.Month)),
                _ => false
            };
            if (ok) return candidato;
        }
        throw new InvalidOperationException("No se encontró una próxima ejecución.");
    }
}

// ---------- Envíos programados (SQL-07 y SQL-08): se configuran desde la ficha de la consulta ----------
public sealed record GuardarProgramacionCommand(string Consulta, string? Codigo, GuardarProgramacionRequest Request) : ICommand<string>;
public sealed record EliminarProgramacionCommand(string Codigo) : ICommand<string>;

internal sealed class ProgramacionHandlers(IConsultaRepository repo, IPermisos permisos, ICurrentUser user, IClock clock) :
    ICommandHandler<GuardarProgramacionCommand, string>, ICommandHandler<EliminarProgramacionCommand, string>
{
    public async Task<Result<string>> Handle(GuardarProgramacionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", "editar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request;
        var destinatarios = (r.Destinatarios ?? []).Select(d => d.Trim().ToLowerInvariant()).Where(d => d.Length > 0).Distinct().ToList();
        var dias = (r.Dias ?? []).Distinct().ToList();
        if (ReglasConsultas.ValidarProgramacion(r.Frecuencia, dias, r.DiaDelMes, r.Formato, destinatarios) is { } v) return Result.Failure<string>(v);
        if (!TimeOnly.TryParseExact(r.Hora ?? "", "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            return Result.Failure<string>(Error.Validation("hora", "Indique la hora como HH:mm."));
        if (await repo.Obtener(c.Consulta, ct) is not { } x) return Result.Failure<string>(Error.NotFound("La consulta"));

        // Los valores fijos y las variables se validan como en una ejecución (las variables se resuelven en cada corrida)
        var parametros = (r.Parametros ?? new Dictionary<string, string?>())
            .Where(kv => x.Consulta.Parametros.Any(p => p.Nombre == kv.Key)).ToDictionary(kv => kv.Key, kv => string.IsNullOrWhiteSpace(kv.Value) ? null : kv.Value.Trim());
        var prueba = ResolverParametros.Resolver(x.Consulta.Parametros,
            parametros.Select(kv => VariablesParametro.EsVariable(kv.Value) ? new ParametroValor(kv.Key, null, kv.Value) : new ParametroValor(kv.Key, kv.Value, null)).ToList(), clock.Hoy);
        if (prueba.IsFailure) return Result.Failure<string>(prueba.Error);

        var frecuencia = Enum.Parse<Frecuencia>(r.Frecuencia);
        var proxima = CalendarioProgramacion.Proxima(frecuencia, hora, dias.Select(d => (DayOfWeek)d).ToList(), r.DiaDelMes, clock.Ahora);
        var req = r with { Destinatarios = destinatarios, Dias = dias, Parametros = parametros, Hora = hora.ToString("HH:mm", CultureInfo.InvariantCulture) };
        var codigo = await repo.GuardarProgramacion(c.Codigo, c.Consulta, req, proxima, Actor.De(user), ct);
        return Result.Success(r.Activa
            ? $"Envío {codigo} guardado: la próxima corrida es el {proxima.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}"
            : $"Envío {codigo} guardado en pausa");
    }

    public async Task<Result<string>> Handle(EliminarProgramacionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("consultas", "editar", ct) is { } e) return Result.Failure<string>(e);
        await repo.EliminarProgramacion(c.Codigo, Actor.De(user), ct);
        return Result.Success($"Envío programado {c.Codigo} eliminado");
    }
}
