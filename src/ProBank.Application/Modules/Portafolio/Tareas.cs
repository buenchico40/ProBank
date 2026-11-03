using ProBank.Application.Abstractions;
using ProBank.Contracts.Portafolio;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.Application.Modules.Portafolio;

public sealed record CrearTareaCommand(CrearTareaRequest Request) : ICommand<TareaCreadaDto>;
public sealed record ActualizarTareaCommand(Guid Token, ActualizarTareaRequest Request) : ICommand<string>;
public sealed record EnviarTareaCommand(Guid Token) : ICommand<string>;
public sealed record AprobarTareaCommand(Guid Token) : ICommand<string>;
public sealed record RechazarTareaCommand(Guid Token, string Comentario) : ICommand<string>;
public sealed record CompletarTareaCommand(Guid Token) : ICommand<string>;
/// <summary>Sin ítem: «Mis tareas» (las del usuario) o, con <see cref="Todas"/> y permiso de edición, todas.</summary>
public sealed record ListarTareasQuery(Guid? ItemToken, bool Todas) : IQuery<IReadOnlyList<TareaDto>>;
public sealed record DetalleTareaQuery(Guid Token) : IQuery<TareaDetalleDto>;
public sealed record ListarAprobacionesQuery : IQuery<IReadOnlyList<AprobacionDto>>;

internal static class Estados
{
    public static string Texto(EstadoTarea e) => e switch
    {
        EstadoTarea.EnRevision => "En revisión", EstadoTarea.EnCurso => "En curso", EstadoTarea.Completada => "Completada", _ => "Pendiente",
    };
}

internal sealed class CrearTareaHandler(ITareaRepository repo, ICurrentUser user, IClock clock, IPermisos permisos)
    : ICommandHandler<CrearTareaCommand, TareaCreadaDto>
{
    public async Task<Result<TareaCreadaDto>> Handle(CrearTareaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("tareas", "crear", ct) is { } e) return Result.Failure<TareaCreadaDto>(e);
        var r = c.Request;
        var itemId = await repo.ObtenerItemId(r.ItemToken, ct);
        if (itemId is null) return Result.Failure<TareaCreadaDto>(Error.NotFound("El proyecto, mejora o bug"));
        var tarea = Tarea.Crear(itemId.Value, r.HitoId, r.Titulo, r.Area, r.TecnicoId, r.Orden, r.FechaInicio, r.FechaTerminacion, r.AprobadorIds, user.UsuarioId, clock.Hoy);
        if (tarea.IsFailure) return Result.Failure<TareaCreadaDto>(tarea.Error);
        return Result.Success(new TareaCreadaDto(tarea.Value.Token, await repo.Crear(tarea.Value, r.DescripcionHtml, Actor.De(user), ct)));
    }
}

internal sealed class ActualizarTareaHandler(ITareaRepository repo, ICurrentUser user, IPermisos permisos) : ICommandHandler<ActualizarTareaCommand, string>
{
    public async Task<Result<string>> Handle(ActualizarTareaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("tareas", "editar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request;
        if (Tarea.Validar(r.Titulo, r.Area, r.Orden, r.FechaInicio, r.FechaTerminacion) is { } v) return Result.Failure<string>(v);
        if (r.Avance is < 0 or > 100) return Result.Failure<string>(Error.Validation("avance", "El avance va de 0 a 100 %."));
        await repo.Actualizar(c.Token, r with { Titulo = r.Titulo.Trim() }, Actor.De(user), ct);
        return Result.Success("Tarea actualizada");
    }
}

/// <summary>El técnico asignado envía y completa su tarea; otros usuarios necesitan poder editar tareas.</summary>
internal abstract class TareaPropiaHandler(ITareaRepository repo, ICurrentUser user, IPermisos permisos)
{
    protected ITareaRepository Repo => repo;
    protected ICurrentUser User => user;

    protected async Task<Result<Tarea>> Cargar(Guid token, CancellationToken ct)
    {
        var t = await repo.Obtener(token, ct);
        if (t is null) return Result.Failure<Tarea>(Error.NotFound("La tarea"));
        if (t.TecnicoId != user.UsuarioId && !await permisos.Tiene("tareas", "editar", ct))
            return Result.Failure<Tarea>(Error.Forbidden("Solo el técnico asignado o quien edita tareas puede hacerlo."));
        return Result.Success(t);
    }
}

internal sealed class EnviarTareaHandler(ITareaRepository repo, ICurrentUser user, IPermisos permisos)
    : TareaPropiaHandler(repo, user, permisos), ICommandHandler<EnviarTareaCommand, string>
{
    public async Task<Result<string>> Handle(EnviarTareaCommand c, CancellationToken ct)
    {
        var t = await Cargar(c.Token, ct);
        if (t.IsFailure) return Result.Failure<string>(t.Error);
        var r = t.Value.EnviarAAprobar();
        if (r.IsFailure) return Result.Failure<string>(r.Error);
        var aprobadores = t.Value.Aprobacion.Aprobadores.Count;
        await Repo.GuardarEstado(t.Value, Actor.De(User), aprobadores > 0 ? "Enviada a aprobar" : "En curso sin aprobación",
            aprobadores > 1 ? $"La deben aprobar sus {aprobadores} aprobadores" : aprobadores == 1 ? "La aprueba su aprobador" : "No tiene aprobadores", ct);
        return Result.Success(Estados.Texto(t.Value.Estado));
    }
}

internal sealed class CompletarTareaHandler(ITareaRepository repo, ICurrentUser user, IPermisos permisos)
    : TareaPropiaHandler(repo, user, permisos), ICommandHandler<CompletarTareaCommand, string>
{
    public async Task<Result<string>> Handle(CompletarTareaCommand c, CancellationToken ct)
    {
        var t = await Cargar(c.Token, ct);
        if (t.IsFailure) return Result.Failure<string>(t.Error);
        var r = t.Value.Completar();
        if (r.IsFailure) return Result.Failure<string>(r.Error);
        await Repo.GuardarEstado(t.Value, Actor.De(User), "Tarea completada", "Avance 100 %", ct);
        return Result.Success(Estados.Texto(t.Value.Estado));
    }
}

internal sealed class AprobarTareaHandler(ITareaRepository repo, ICurrentUser user, IClock clock, IPermisos permisos) : ICommandHandler<AprobarTareaCommand, string>
{
    public async Task<Result<string>> Handle(AprobarTareaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("aprobaciones", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        var t = await repo.Obtener(c.Token, ct);
        if (t is null) return Result.Failure<string>(Error.NotFound("La tarea"));
        var r = t.Aprobar(user.UsuarioId, clock.Ahora);
        if (r.IsFailure) return Result.Failure<string>(r.Error);
        var avance = $"{t.Aprobacion.Aprobadas} de {t.Aprobacion.Aprobadores.Count}";
        await repo.GuardarEstado(t, Actor.De(user), "Aprobación", t.Aprobacion.Completa ? $"{avance}: aprobada por todos, pasa a En curso" : avance, ct);
        return Result.Success(avance);
    }
}

internal sealed class RechazarTareaHandler(ITareaRepository repo, ICurrentUser user, IClock clock, IPermisos permisos) : ICommandHandler<RechazarTareaCommand, string>
{
    public async Task<Result<string>> Handle(RechazarTareaCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("aprobaciones", "aprobar", ct) is { } e) return Result.Failure<string>(e);
        var t = await repo.Obtener(c.Token, ct);
        if (t is null) return Result.Failure<string>(Error.NotFound("La tarea"));
        var r = t.Rechazar(user.UsuarioId, c.Comentario, clock.Ahora);
        if (r.IsFailure) return Result.Failure<string>(r.Error);
        await repo.GuardarEstado(t, Actor.De(user), "Rechazo", c.Comentario.Trim(), ct);
        return Result.Success(Estados.Texto(t.Estado));
    }
}

internal sealed class ListarTareasHandler(ITareaRepository repo, ICurrentUser user, IPermisos permisos) : IQueryHandler<ListarTareasQuery, IReadOnlyList<TareaDto>>
{
    public async Task<Result<IReadOnlyList<TareaDto>>> Handle(ListarTareasQuery q, CancellationToken ct)
    {
        if (q.ItemToken is not null)
            return await permisos.Tiene("proyectos", "ver", ct) || await permisos.Tiene("tareas", "ver", ct)
                ? Result.Success(await repo.Listar(q.ItemToken, null, ct)) : Result.Failure<IReadOnlyList<TareaDto>>(Acceso.SinPermiso);
        if (await permisos.Exigir("tareas", "ver", ct) is { } e) return Result.Failure<IReadOnlyList<TareaDto>>(e);
        var todas = q.Todas && await permisos.Tiene("tareas", "editar", ct);
        return Result.Success(await repo.Listar(null, todas ? null : user.UsuarioId, ct));
    }
}

internal sealed class DetalleTareaHandler(ITareaRepository repo, IPermisos permisos) : IQueryHandler<DetalleTareaQuery, TareaDetalleDto>
{
    public async Task<Result<TareaDetalleDto>> Handle(DetalleTareaQuery q, CancellationToken ct) =>
        await permisos.Tiene("tareas", "ver", ct) || await permisos.Tiene("aprobaciones", "ver", ct) || await permisos.Tiene("proyectos", "ver", ct)
            ? Result.Success(await repo.Detalle(q.Token, ct)) : Result.Failure<TareaDetalleDto>(Acceso.SinPermiso);
}

internal sealed class ListarAprobacionesHandler(ITareaRepository repo, ICurrentUser user, IPermisos permisos) : IQueryHandler<ListarAprobacionesQuery, IReadOnlyList<AprobacionDto>>
{
    public async Task<Result<IReadOnlyList<AprobacionDto>>> Handle(ListarAprobacionesQuery q, CancellationToken ct) =>
        await permisos.Exigir("aprobaciones", "ver", ct) is { } e
            ? Result.Failure<IReadOnlyList<AprobacionDto>>(e)
            : Result.Success(await repo.ListarAprobaciones(user.UsuarioId, ct));
}
