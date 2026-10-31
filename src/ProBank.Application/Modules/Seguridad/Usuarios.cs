using ProBank.Application.Abstractions;
using ProBank.Contracts.Seguridad;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Seguridad;

namespace ProBank.Application.Modules.Seguridad;

public sealed record ListarUsuariosQuery : IQuery<IReadOnlyList<UsuarioDto>>;
public sealed record ListarIngresosQuery(Guid Token) : IQuery<IReadOnlyList<IngresoUsuarioDto>>;
public sealed record CrearUsuarioCommand(CrearUsuarioRequest Request) : ICommand<ClaveTemporalDto>;
public sealed record ActualizarUsuarioCommand(Guid Token, ActualizarUsuarioRequest Request) : ICommand<string>;
public sealed record RestablecerClaveCommand(Guid Token) : ICommand<ClaveTemporalDto>;
public sealed record DesbloquearUsuarioCommand(Guid Token) : ICommand<string>;
public sealed record RevocarSesionesCommand(Guid Token) : ICommand<string>;

internal sealed class ListarUsuariosHandler(ISeguridadRepository repo, IPermisos permisos) : IQueryHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>>
{
    public async Task<Result<IReadOnlyList<UsuarioDto>>> Handle(ListarUsuariosQuery q, CancellationToken ct) =>
        await permisos.Exigir("usuarios", "ver", ct) is { } e
            ? Result.Failure<IReadOnlyList<UsuarioDto>>(e)
            : Result.Success(await repo.ListarUsuarios(ct));
}

internal sealed class ListarIngresosHandler(ISeguridadRepository repo, IPermisos permisos) : IQueryHandler<ListarIngresosQuery, IReadOnlyList<IngresoUsuarioDto>>
{
    public async Task<Result<IReadOnlyList<IngresoUsuarioDto>>> Handle(ListarIngresosQuery q, CancellationToken ct) =>
        await permisos.Exigir("usuarios", "ver", ct) is { } e
            ? Result.Failure<IReadOnlyList<IngresoUsuarioDto>>(e)
            : Result.Success(await repo.ListarIngresos(q.Token, ct));
}

/// <summary>SEG-01 y SEG-07: crea el usuario con una clave temporal generada aquí, que se muestra una sola vez.</summary>
internal sealed class CrearUsuarioHandler(ISeguridadRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<CrearUsuarioCommand, ClaveTemporalDto>
{
    public async Task<Result<ClaveTemporalDto>> Handle(CrearUsuarioCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("usuarios", "crear", ct) is { } e) return Result.Failure<ClaveTemporalDto>(e);
        var r = c.Request with { Usuario = c.Request.Usuario.Trim().ToLowerInvariant(), Nombre = c.Request.Nombre.Trim(), Correo = c.Request.Correo?.Trim() };
        r = r with { RolPredeterminado = r.RolPredeterminado ?? r.Roles.FirstOrDefault() };
        if ((ReglasSeguridad.ValidarUsuario(r.Usuario) ?? ReglasSeguridad.ValidarDatos(r.Nombre, r.Correo, r.Roles, r.RolPredeterminado, "Activo")) is { } v)
            return Result.Failure<ClaveTemporalDto>(v);

        var temporal = Claves.GenerarTemporal();
        var (hash, sal) = Claves.Generar(temporal);
        var token = await repo.CrearUsuario(r, hash, sal, Actor.De(user), ct);
        return Result.Success(new ClaveTemporalDto(token, r.Usuario, temporal));
    }
}

internal sealed class ActualizarUsuarioHandler(ISeguridadRepository repo, IPermisos permisos, ICurrentUser user, ISesiones sesiones)
    : ICommandHandler<ActualizarUsuarioCommand, string>
{
    public async Task<Result<string>> Handle(ActualizarUsuarioCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("usuarios", "editar", ct) is { } e) return Result.Failure<string>(e);
        var r = c.Request with { Nombre = c.Request.Nombre.Trim(), Correo = c.Request.Correo?.Trim() };
        r = r with { RolPredeterminado = r.RolPredeterminado ?? r.Roles.FirstOrDefault() };
        if (ReglasSeguridad.ValidarDatos(r.Nombre, r.Correo, r.Roles, r.RolPredeterminado, r.Estado) is { } v) return Result.Failure<string>(v);

        var usuarioId = await repo.ActualizarUsuario(c.Token, r, Actor.De(user), ct);
        sesiones.Invalidar(usuarioId);
        return Result.Success("Usuario actualizado");
    }
}

internal sealed class RestablecerClaveHandler(ISeguridadRepository repo, IPermisos permisos, ICurrentUser user, ISesiones sesiones)
    : ICommandHandler<RestablecerClaveCommand, ClaveTemporalDto>
{
    public async Task<Result<ClaveTemporalDto>> Handle(RestablecerClaveCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("usuarios", "clave", ct) is { } e) return Result.Failure<ClaveTemporalDto>(e);
        var temporal = Claves.GenerarTemporal();
        var (hash, sal) = Claves.Generar(temporal);
        var usuarioId = await repo.RestablecerClave(c.Token, hash, sal, Actor.De(user), ct);
        sesiones.Invalidar(usuarioId);
        var usuario = (await repo.ListarUsuarios(ct)).FirstOrDefault(u => u.Token == c.Token)?.Usuario ?? "";
        return Result.Success(new ClaveTemporalDto(c.Token, usuario, temporal));
    }
}

internal sealed class DesbloquearUsuarioHandler(ISeguridadRepository repo, IPermisos permisos, ICurrentUser user) : ICommandHandler<DesbloquearUsuarioCommand, string>
{
    public async Task<Result<string>> Handle(DesbloquearUsuarioCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("usuarios", "desbloquear", ct) is { } e) return Result.Failure<string>(e);
        await repo.Desbloquear(c.Token, Actor.De(user), ct);
        return Result.Success("Usuario desbloqueado");
    }
}

internal sealed class RevocarSesionesHandler(ISeguridadRepository repo, IPermisos permisos, ICurrentUser user, ISesiones sesiones)
    : ICommandHandler<RevocarSesionesCommand, string>
{
    public async Task<Result<string>> Handle(RevocarSesionesCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("usuarios", "revocar", ct) is { } e) return Result.Failure<string>(e);
        sesiones.Invalidar(await repo.RevocarSesiones(c.Token, Actor.De(user), ct));
        return Result.Success("Sesiones revocadas");
    }
}
