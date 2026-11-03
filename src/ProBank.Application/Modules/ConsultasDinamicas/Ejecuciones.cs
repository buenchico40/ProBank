using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Common;

namespace ProBank.Application.Modules.ConsultasDinamicas;

// ---------- Consultas ----------
public sealed record ConsultasDisponiblesQuery : IQuery<IReadOnlyList<ConsultaDisponibleDto>>;
public sealed record ConsultaParaEjecutarQuery(string Codigo) : IQuery<ConsultaParaEjecutarDto>;
public sealed record ListarEjecucionesQuery : IQuery<IReadOnlyList<EjecucionDto>>;
public sealed record DetalleEjecucionQuery(string Codigo) : IQuery<EjecucionDetalleDto>;
public sealed record ResultadoEjecucionQuery(string Codigo) : IQuery<ResultadoConsultaDto>;
public sealed record AuditoriaEjecucionesQuery(DateOnly Desde, DateOnly Hasta) : IQuery<IReadOnlyList<EventoEjecucionDto>>;

// ---------- Comandos ----------
public sealed record EjecutarConsultaCommand(EjecutarConsultaRequest Request) : ICommand<ResultadoConsultaDto>;
public sealed record EjecutarAprobadaCommand(string Codigo) : ICommand<ResultadoConsultaDto>;
public sealed record ResolverEjecucionCommand(string Codigo, ResolverEjecucionRequest Request) : ICommand<string>;
/// <summary>SQL-05: exporta el resultado guardado de una ejecución y deja el evento en la auditoría.</summary>
public sealed record ExportarEjecucionCommand(string Codigo, string Formato) : ICommand<ArchivoAdjunto>;

internal static class Ejecuciones
{
    /// <summary>Ven una ejecución quien la solicitó, sus aprobadores y quien audita ejecuciones.</summary>
    public static async Task<bool> PuedeVer(EjecucionInterna e, ICurrentUser user, IPermisos permisos, CancellationToken ct) =>
        e.SolicitanteId == user.UsuarioId || e.Aprobaciones.Any(a => a.UsuarioId == user.UsuarioId) || await permisos.Tiene("audejec", "ver", ct);

    public static ResultadoConsultaDto Dto(string codigo, string estado, ResultadoEjecucion? r, string parametros) =>
        new(codigo, MotorConsultas.Estado(estado), r?.Columnas ?? [], r?.Tipos ?? [], r?.Filas ?? [], r?.DuracionMs ?? 0, r?.Truncado ?? false, parametros);
}

internal sealed class EjecucionConsultas(IConsultaRepository repo, IPermisos permisos, ICurrentUser user, IClock clock, IAlmacenArchivos almacen) :
    IQueryHandler<ConsultasDisponiblesQuery, IReadOnlyList<ConsultaDisponibleDto>>,
    IQueryHandler<ConsultaParaEjecutarQuery, ConsultaParaEjecutarDto>,
    IQueryHandler<ListarEjecucionesQuery, IReadOnlyList<EjecucionDto>>,
    IQueryHandler<DetalleEjecucionQuery, EjecucionDetalleDto>,
    IQueryHandler<ResultadoEjecucionQuery, ResultadoConsultaDto>,
    IQueryHandler<AuditoriaEjecucionesQuery, IReadOnlyList<EventoEjecucionDto>>
{
    public async Task<Result<IReadOnlyList<ConsultaDisponibleDto>>> Handle(ConsultasDisponiblesQuery q, CancellationToken ct) =>
        await permisos.Exigir("ejecuciones", "crear", ct) is { } e ? Result.Failure<IReadOnlyList<ConsultaDisponibleDto>>(e)
            : Result.Success(await repo.Disponibles(user.RolActivo, ct));

    public async Task<Result<ConsultaParaEjecutarDto>> Handle(ConsultaParaEjecutarQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("ejecuciones", "crear", ct) is { } e) return Result.Failure<ConsultaParaEjecutarDto>(e);
        if (await repo.Obtener(q.Codigo, ct) is not { } x) return Result.Failure<ConsultaParaEjecutarDto>(Error.NotFound("La consulta"));
        var c = x.Consulta;
        if (!c.Publicada) return Result.Failure<ConsultaParaEjecutarDto>(Error.Validation("no_publicada", $"Solo se ejecutan consultas publicadas; publique {c.Codigo} primero."));
        if (!c.RolPuedeEjecutar(user.RolActivo)) return Result.Failure<ConsultaParaEjecutarDto>(Error.Forbidden($"Su rol activo ({user.RolActivo}) no puede ejecutar {c.Codigo}."));
        return Result.Success(new ConsultaParaEjecutarDto(c.Codigo, c.Nombre, c.Procedimiento, c.TiempoMaximoSeg, c.LimiteFilas, x.Conexiones,
            x.NombresAprobadores, c.Parametros.Select(p => new ParametroDto(p.Nombre, p.Etiqueta, p.Tipo.ToString(), null, p.Obligatorio, p.ValorDefecto, p.Opciones)).ToList(),
            ResolverParametros.Variables(clock.Hoy)));
    }

    public async Task<Result<IReadOnlyList<EjecucionDto>>> Handle(ListarEjecucionesQuery q, CancellationToken ct) =>
        await permisos.Exigir("ejecuciones", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<EjecucionDto>>(e)
            : Result.Success(await repo.ListarEjecuciones(user.UsuarioId, await permisos.Tiene("audejec", "ver", ct), ct));

    public async Task<Result<EjecucionDetalleDto>> Handle(DetalleEjecucionQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("ejecuciones", "ver", ct) is { } e) return Result.Failure<EjecucionDetalleDto>(e);
        if (await repo.ObtenerEjecucion(q.Codigo, ct) is not { } x || !await Ejecuciones.PuedeVer(x, user, permisos, ct))
            return Result.Failure<EjecucionDetalleDto>(Error.NotFound("La ejecución"));
        var meToca = x.Estado == "PendienteAprobacion" && x.Aprobaciones.Any(a => a.UsuarioId == user.UsuarioId && a.Aprobacion.Decision is null);
        return Result.Success(new EjecucionDetalleDto(x.Codigo, x.ConsultaCodigo, x.ConsultaNombre, x.Procedimiento, x.Base, x.SolicitadaPor, x.Fecha,
            x.ParametrosResueltos, x.Programada, x.Filas, x.DuracionMs, x.Truncado, x.Mensaje, MotorConsultas.Estado(x.Estado),
            x.SolicitanteId == user.UsuarioId, meToca, x.RutaResultado is not null, x.Aprobaciones.Select(a => a.Aprobacion).ToList(), x.Eventos));
    }

    // Los datos del resultado los ve solo quien solicitó la ejecución (los aprobadores autorizan, no consultan)
    public async Task<Result<ResultadoConsultaDto>> Handle(ResultadoEjecucionQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("ejecuciones", "ver", ct) is { } e) return Result.Failure<ResultadoConsultaDto>(e);
        if (await repo.ObtenerEjecucion(q.Codigo, ct) is not { } x || x.SolicitanteId != user.UsuarioId)
            return Result.Failure<ResultadoConsultaDto>(Error.NotFound("La ejecución"));
        if (x.RutaResultado is null || await MotorConsultas.Leer(almacen, x.RutaResultado, ct) is not { } r)
            return Result.Failure<ResultadoConsultaDto>(Error.Validation("sin_resultado", $"La ejecución {x.Codigo} está {MotorConsultas.Estado(x.Estado).ToLowerInvariant()}: no tiene resultados."));
        return Result.Success(Ejecuciones.Dto(x.Codigo, x.Estado, r, x.ParametrosResueltos));
    }

    public async Task<Result<IReadOnlyList<EventoEjecucionDto>>> Handle(AuditoriaEjecucionesQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("audejec", "ver", ct) is { } e) return Result.Failure<IReadOnlyList<EventoEjecucionDto>>(e);
        if (q.Hasta < q.Desde || q.Hasta.DayNumber - q.Desde.DayNumber > 366)
            return Result.Failure<IReadOnlyList<EventoEjecucionDto>>(Error.Validation("periodo", "El periodo no es válido (máximo un año)."));
        return Result.Success(await repo.ListarEventos(q.Desde, q.Hasta, ct));
    }
}

internal sealed class EjecucionComandos(IConsultaRepository repo, IConsultaExecutor executor, IAlmacenArchivos almacen, IPermisos permisos,
    ICurrentUser user, IClock clock, IEnumerable<IExportador> exportadores) :
    ICommandHandler<EjecutarConsultaCommand, ResultadoConsultaDto>,
    ICommandHandler<EjecutarAprobadaCommand, ResultadoConsultaDto>,
    ICommandHandler<ResolverEjecucionCommand, string>,
    ICommandHandler<ExportarEjecucionCommand, ArchivoAdjunto>
{
    /// <summary>SQL-01 a SQL-04 y SQL-06: registra la solicitud; sin aprobadores se ejecuta al momento, con aprobadores queda pendiente.</summary>
    public async Task<Result<ResultadoConsultaDto>> Handle(EjecutarConsultaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("ejecuciones", "crear", ct) is { } e) return Result.Failure<ResultadoConsultaDto>(e);
        var req = c.Request;
        if (await repo.Obtener(req.ConsultaCodigo ?? "", ct) is not { } x) return Result.Failure<ResultadoConsultaDto>(Error.NotFound("La consulta"));
        var consulta = x.Consulta;
        var puede = consulta.PuedeEjecutar(user.RolActivo, req.Conexion ?? "");
        if (puede.IsFailure) return Result.Failure<ResultadoConsultaDto>(puede.Error);

        var p = ResolverParametros.Resolver(consulta.Parametros, req.Parametros ?? [], clock.Hoy);
        if (p.IsFailure) return Result.Failure<ResultadoConsultaDto>(p.Error);

        var aprobadores = consulta.Aprobadores.Where(a => a != user.UsuarioId).ToList();
        var pendiente = aprobadores.Count > 0;
        var (id, codigo) = await repo.CrearEjecucion(consulta.ConsultaId, req.Conexion!, user.UsuarioId, aprobadores, p.Value.Texto, p.Value.Json, false, pendiente, ct);
        await repo.RegistrarEvento(id, "Solicitud", p.Value.Texto, pendiente ? "Pendiente de aprobación" : "Registrada", null, null, ct);
        if (pendiente) return Result.Success(Ejecuciones.Dto(codigo, "PendienteAprobacion", null, p.Value.Texto));

        var r = await MotorConsultas.Correr(repo, executor, almacen, consulta, id, codigo, req.Conexion!, p.Value.Valores, p.Value.Texto, ct);
        return r.IsFailure ? Result.Failure<ResultadoConsultaDto>(r.Error) : Result.Success(Ejecuciones.Dto(codigo, "Ejecutada", r.Value, p.Value.Texto));
    }

    /// <summary>SQL-03: el solicitante ejecuta una vez su ejecución aprobada por todos, con los valores que se aprobaron.</summary>
    public async Task<Result<ResultadoConsultaDto>> Handle(EjecutarAprobadaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("ejecuciones", "crear", ct) is { } e) return Result.Failure<ResultadoConsultaDto>(e);
        if (await repo.ObtenerEjecucion(c.Codigo, ct) is not { } x || x.SolicitanteId != user.UsuarioId)
            return Result.Failure<ResultadoConsultaDto>(Error.NotFound("La ejecución"));
        if (x.Estado != "Aprobada")
            return Result.Failure<ResultadoConsultaDto>(Error.Validation("estado", x.Estado == "PendienteAprobacion"
                ? "Faltan aprobaciones: " + string.Join(", ", x.Aprobaciones.Where(a => a.Aprobacion.Decision is null).Select(a => a.Aprobacion.Aprobador))
                : $"La ejecución {x.Codigo} está {MotorConsultas.Estado(x.Estado).ToLowerInvariant()}: solicite una nueva."));
        if (await repo.Obtener(x.ConsultaCodigo, ct) is not { } q) return Result.Failure<ResultadoConsultaDto>(Error.NotFound("La consulta"));
        var puede = q.Consulta.PuedeEjecutar(user.RolActivo, x.ConexionCodigo);
        if (puede.IsFailure) return Result.Failure<ResultadoConsultaDto>(puede.Error);

        var valores = ResolverParametros.DesdeJson(q.Consulta.Parametros, x.ParametrosJson);
        var r = await MotorConsultas.Correr(repo, executor, almacen, q.Consulta, x.EjecucionId, x.Codigo, x.ConexionCodigo, valores, x.ParametrosResueltos, ct);
        return r.IsFailure ? Result.Failure<ResultadoConsultaDto>(r.Error) : Result.Success(Ejecuciones.Dto(x.Codigo, "Ejecutada", r.Value, x.ParametrosResueltos));
    }

    public async Task<Result<string>> Handle(ResolverEjecucionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("ejecuciones", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        var comentario = c.Request.Comentario?.Trim();
        if (!c.Request.Aprobar && string.IsNullOrEmpty(comentario)) return Result.Failure<string>(Error.Validation("comentario", "Indique el motivo del rechazo."));
        if (comentario?.Length > 1000) return Result.Failure<string>(Error.Validation("comentario", "El comentario admite hasta 1000 caracteres."));
        var estado = await repo.ResolverEjecucion(c.Codigo, user.UsuarioId, c.Request.Aprobar, comentario, user.Ip, ct);
        return Result.Success(estado switch
        {
            "Aprobada" => $"Ejecución {c.Codigo} aprobada por todos: el solicitante ya puede ejecutarla",
            "Rechazada" => $"Ejecución {c.Codigo} rechazada",
            _ => $"Aprobación registrada: la ejecución {c.Codigo} espera a los demás aprobadores",
        });
    }

    public async Task<Result<ArchivoAdjunto>> Handle(ExportarEjecucionCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("ejecuciones", "exportar", ct) is { } e) return Result.Failure<ArchivoAdjunto>(e);
        var exp = exportadores.FirstOrDefault(x => x.Formato == c.Formato);
        if (exp is null) return Result.Failure<ArchivoAdjunto>(Error.Validation("formato", "El formato es Excel, CSV, PDF, JSON o texto."));
        if (await repo.ObtenerEjecucion(c.Codigo, ct) is not { } x || x.SolicitanteId != user.UsuarioId)
            return Result.Failure<ArchivoAdjunto>(Error.NotFound("La ejecución"));
        if (x.RutaResultado is null || await MotorConsultas.Leer(almacen, x.RutaResultado, ct) is not { } r)
            return Result.Failure<ArchivoAdjunto>(Error.Validation("sin_resultado", $"La ejecución {x.Codigo} no tiene resultados para exportar."));
        var archivo = exp.Exportar(r, $"{x.ConsultaCodigo} · {x.ConsultaNombre} · {x.Codigo}");
        var nombre = $"{x.ConsultaCodigo}_{x.Codigo}.{exp.Formato}";
        await repo.RegistrarEvento(x.EjecucionId, "Exportación " + exp.Formato.ToUpperInvariant(), nombre, "Exitosa", r.Filas.Count, null, ct);
        return Result.Success(new ArchivoAdjunto(nombre, exp.TipoContenido, new MemoryStream(archivo, writable: false)));
    }
}
