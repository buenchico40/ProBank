using System.Globalization;
using ProBank.Application.Abstractions;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.Application.Modules.ConsultasDinamicas;

public sealed record EjecutarConsultaCommand(EjecutarConsultaRequest Request) : ICommand<ResultadoConsultaDto>;

internal sealed class EjecutarConsultaHandler(IConsultaRepository repo, IConsultaExecutor executor, ICurrentUser user, IClock clock)
    : ICommandHandler<EjecutarConsultaCommand, ResultadoConsultaDto>
{
    public async Task<Result<ResultadoConsultaDto>> Handle(EjecutarConsultaCommand c, CancellationToken ct)
    {
        var req = c.Request;
        var consulta = await repo.Obtener(req.ConsultaCodigo, ct);
        if (consulta is null) return Result.Failure<ResultadoConsultaDto>(Error.NotFound("La consulta"));
        var puede = consulta.PuedeEjecutar(user.RolActivo, req.ConexionId);
        if (puede.IsFailure) return Result.Failure<ResultadoConsultaDto>(puede.Error);

        var valores = ResolverParametros.Resolver(consulta.Parametros, req.Parametros, clock.Hoy);
        if (valores.IsFailure) return Result.Failure<ResultadoConsultaDto>(valores.Error);
        var (parametros, texto) = valores.Value;

        var aprobadores = consulta.Aprobadores.Where(a => a != user.UsuarioId).ToList();
        var (id, codigo) = await repo.CrearEjecucion(consulta.ConsultaId, req.ConexionId, user.UsuarioId, aprobadores, texto, aprobadores.Count > 0, ct);
        await repo.RegistrarEvento(id, "Solicitud", texto, aprobadores.Count > 0 ? "Pendiente de aprobación" : "Registrada", null, null, ct);
        if (aprobadores.Count > 0)
            return Result.Success(new ResultadoConsultaDto(codigo, [], [], 0, false));

        try
        {
            var r = await executor.Ejecutar(req.ConexionId, consulta.Procedimiento, parametros, consulta.TiempoMaximoSeg, consulta.LimiteFilas, ct);
            await repo.CerrarEjecucion(id, r.Filas.Count, r.DuracionMs, "Ejecutada", ct);
            await repo.RegistrarEvento(id, "Ejecucion", $"EXEC {consulta.Procedimiento}", "Exitosa", r.Filas.Count, r.DuracionMs, ct);
            return Result.Success(new ResultadoConsultaDto(codigo, r.Columnas, r.Filas, r.DuracionMs, r.Truncado));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await repo.RegistrarEvento(id, "Ejecucion", ex.Message, "Fallida", null, null, CancellationToken.None);
            return Result.Failure<ResultadoConsultaDto>(new Error("ejecucion_fallida", "La consulta no se pudo ejecutar; quedó registrado en la auditoría."));
        }
    }
}

public static class ResolverParametros
{
    /// <summary>Aplica valores enviados, valores por defecto y variables (SQL-04); devuelve también el texto auditable.</summary>
    public static Result<(IReadOnlyDictionary<string, object?> Valores, string Texto)> Resolver(
        IReadOnlyList<ParametroConsulta> definicion, IReadOnlyList<ParametroValor> enviados, DateOnly hoy)
    {
        var valores = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var partes = new List<string>();
        foreach (var p in definicion)
        {
            var e = enviados.FirstOrDefault(x => string.Equals(x.Nombre, p.Nombre, StringComparison.OrdinalIgnoreCase));
            var origen = e?.Variable ?? e?.Valor ?? p.ValorDefecto;
            var resuelto = VariablesParametro.Resolver(origen, hoy);
            if (p.Obligatorio && (resuelto is null || resuelto is string { Length: 0 }))
                return Result.Failure<(IReadOnlyDictionary<string, object?>, string)>(Error.Validation("parametro", $"{p.Etiqueta} es obligatorio."));
            var tipado = Convertir(resuelto, p.Tipo);
            valores[p.Nombre] = tipado;
            var mostrado = tipado is DateOnly d ? d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : tipado?.ToString() ?? "(vacío)";
            partes.Add(VariablesParametro.EsVariable(origen) ? $"{p.Etiqueta}={origen} → {mostrado}" : $"{p.Etiqueta}={mostrado}");
        }
        return Result.Success<(IReadOnlyDictionary<string, object?>, string)>((valores, string.Join("; ", partes)));
    }

    private static object? Convertir(object? v, TipoParametro tipo) => (v, tipo) switch
    {
        (null, _) => null,
        (DateOnly d, _) => d,
        (int i, _) => i,
        (string s, TipoParametro.Fecha) when DateOnly.TryParse(s, CultureInfo.GetCultureInfo("es-EC"), out var d) => d,
        (string s, TipoParametro.Numero) when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) => n,
        _ => v
    };
}
