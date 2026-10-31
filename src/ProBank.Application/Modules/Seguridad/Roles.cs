using ProBank.Application.Abstractions;
using ProBank.Contracts.Seguridad;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Seguridad;

namespace ProBank.Application.Modules.Seguridad;

public sealed record ListarRolesQuery : IQuery<IReadOnlyList<RolDto>>;
public sealed record GuardarRolCommand(int? RolId, GuardarRolRequest Request) : ICommand<int>;
public sealed record ListarPermisosQuery(int RolId) : IQuery<IReadOnlyList<PermisoDto>>;
public sealed record GuardarPermisosCommand(int RolId, IReadOnlyList<string> Permisos) : ICommand<string>;
public sealed record ListarAuditoriaQuery(DateOnly? Desde, DateOnly? Hasta, string? Modulo) : IQuery<IReadOnlyList<AuditoriaDto>>;
public sealed record ListarParametrosQuery : IQuery<IReadOnlyList<ParametroDto>>;
public sealed record GuardarParametrosCommand(IReadOnlyList<ValorParametro> Valores) : ICommand<string>;

internal sealed class ListarRolesHandler(ISeguridadRepository repo, IPermisos permisos) : IQueryHandler<ListarRolesQuery, IReadOnlyList<RolDto>>
{
    // Los formularios de usuarios también listan roles: basta con ver roles o usuarios.
    public async Task<Result<IReadOnlyList<RolDto>>> Handle(ListarRolesQuery q, CancellationToken ct) =>
        await permisos.Tiene("roles", "ver", ct) || await permisos.Tiene("usuarios", "ver", ct)
            ? Result.Success(await repo.ListarRoles(ct))
            : Result.Failure<IReadOnlyList<RolDto>>(Acceso.SinPermiso);
}

internal sealed class GuardarRolHandler(ISeguridadRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarRolCommand, int>
{
    public async Task<Result<int>> Handle(GuardarRolCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("roles", c.RolId is null ? "crear" : "editar", ct) is { } e) return Result.Failure<int>(e);
        var r = c.Request with { Nombre = c.Request.Nombre.Trim(), Descripcion = c.Request.Descripcion?.Trim() };
        if (ReglasSeguridad.ValidarRol(r.Nombre, r.Descripcion, r.Estado) is { } v) return Result.Failure<int>(v);
        return Result.Success(await repo.GuardarRol(c.RolId, r, Actor.De(user), ct));
    }
}

internal sealed class ListarPermisosHandler(ISeguridadRepository repo, IPermisos permisos) : IQueryHandler<ListarPermisosQuery, IReadOnlyList<PermisoDto>>
{
    public async Task<Result<IReadOnlyList<PermisoDto>>> Handle(ListarPermisosQuery q, CancellationToken ct) =>
        await permisos.Exigir("roles", "ver", ct) is { } e
            ? Result.Failure<IReadOnlyList<PermisoDto>>(e)
            : Result.Success(await repo.ListarPermisos(q.RolId, ct));
}

internal sealed class GuardarPermisosHandler(ISeguridadRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarPermisosCommand, string>
{
    public async Task<Result<string>> Handle(GuardarPermisosCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("roles", "editar", ct) is { } e) return Result.Failure<string>(e);
        if (c.Permisos.Any(p => p.Contains(',') || !p.Contains(':')))
            return Result.Failure<string>(Error.Validation("permisos", "Cada permiso debe tener la forma opcion:accion."));
        await repo.GuardarPermisos(c.RolId, c.Permisos, Actor.De(user), ct);
        return Result.Success("Permisos guardados");
    }
}

/// <summary>SEG-05: la auditoría solo se consulta. Sin fechas, los últimos 7 días; el rango máximo es de un año.</summary>
internal sealed class ListarAuditoriaHandler(ISeguridadRepository repo, IPermisos permisos, IClock clock) : IQueryHandler<ListarAuditoriaQuery, IReadOnlyList<AuditoriaDto>>
{
    public async Task<Result<IReadOnlyList<AuditoriaDto>>> Handle(ListarAuditoriaQuery q, CancellationToken ct)
    {
        if (await permisos.Exigir("auditoria", "ver", ct) is { } e) return Result.Failure<IReadOnlyList<AuditoriaDto>>(e);
        var hasta = q.Hasta ?? clock.Hoy;
        var desde = q.Desde ?? hasta.AddDays(-7);
        if (desde > hasta || hasta.DayNumber - desde.DayNumber > 366)
            return Result.Failure<IReadOnlyList<AuditoriaDto>>(Error.Validation("rango", "El rango de fechas no es válido (máximo un año)."));
        return Result.Success(await repo.ListarAuditoria(desde, hasta, string.IsNullOrWhiteSpace(q.Modulo) ? null : q.Modulo, ct));
    }
}

internal sealed class ListarParametrosHandler(ISeguridadRepository repo, IPermisos permisos) : IQueryHandler<ListarParametrosQuery, IReadOnlyList<ParametroDto>>
{
    public async Task<Result<IReadOnlyList<ParametroDto>>> Handle(ListarParametrosQuery q, CancellationToken ct) =>
        await permisos.Exigir("params", "ver", ct) is { } e
            ? Result.Failure<IReadOnlyList<ParametroDto>>(e)
            : Result.Success(await repo.ListarParametros(ct));
}

internal sealed class GuardarParametrosHandler(ISeguridadRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<GuardarParametrosCommand, string>
{
    public async Task<Result<string>> Handle(GuardarParametrosCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("params", "editar", ct) is { } e) return Result.Failure<string>(e);
        await repo.GuardarParametros(c.Valores, Actor.De(user), ct);
        return Result.Success("Parámetros de acceso guardados");
    }
}
