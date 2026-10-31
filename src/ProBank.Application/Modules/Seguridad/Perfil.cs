using ProBank.Application.Abstractions;
using ProBank.Contracts.Seguridad;
using ProBank.Domain.Common;

namespace ProBank.Application.Modules.Seguridad;

public sealed record ObtenerPerfilQuery : IQuery<PerfilDto>;
public sealed record CambiarClaveCommand(CambiarClaveRequest Request) : ICommand<string>;
public sealed record CambiarRolCommand(CambiarRolRequest Request) : ICommand<string>;

/// <summary>Perfil del usuario conectado, con los permisos de su rol activo (la Web arma el menú con "ver").</summary>
internal sealed class ObtenerPerfilHandler(ISeguridadRepository repo, ICurrentUser user) : IQueryHandler<ObtenerPerfilQuery, PerfilDto>
{
    public async Task<Result<PerfilDto>> Handle(ObtenerPerfilQuery q, CancellationToken ct) =>
        await repo.ObtenerPerfil(user.UsuarioId, user.RolActivo, ct) is { } p
            ? Result.Success(p)
            : Result.Failure<PerfilDto>(Error.NotFound("El usuario"));
}

/// <summary>
/// El usuario cambia su propia clave: exige la actual, aplica la política (SEG-06) y revoca sus sesiones (SEG-07),
/// así que debe volver a ingresar con la nueva.
/// </summary>
internal sealed class CambiarClaveHandler(ICredencialesRepository credenciales, ISeguridadRepository repo, ICurrentUser user, ISesiones sesiones)
    : ICommandHandler<CambiarClaveCommand, string>
{
    public static readonly Error ActualIncorrecta = Error.Validation("clave_actual", "La contraseña actual no es correcta.");

    public async Task<Result<string>> Handle(CambiarClaveCommand c, CancellationToken ct)
    {
        var u = await credenciales.Obtener(user.Usuario, ct);
        if (u is null || u.UsuarioId != user.UsuarioId) return Result.Failure<string>(Error.NotFound("El usuario"));
        if (u.ClaveHash is null || u.ClaveSal is null || !Claves.Verificar(c.Request.Actual, u.ClaveHash, u.ClaveSal))
            return Result.Failure<string>(ActualIncorrecta);
        if (Claves.ValidarNueva(c.Request.Nueva, u.Usuario) is { } error) return Result.Failure<string>(Error.Validation("clave_nueva", error));
        if (c.Request.Nueva == c.Request.Actual) return Result.Failure<string>(Error.Validation("clave_nueva", "La nueva contraseña debe ser distinta de la actual."));

        var (hash, sal) = Claves.Generar(c.Request.Nueva);
        await repo.CambiarClave(u.UsuarioId, hash, sal, user.RolActivo, user.Ip, ct);
        sesiones.Invalidar(u.UsuarioId);
        return Result.Success("Contraseña cambiada");
    }
}

/// <summary>SEG-01: el rol activo debe estar entre los asignados; puede quedar como predeterminado.</summary>
internal sealed class CambiarRolHandler(ISeguridadRepository repo, ICurrentUser user) : ICommandHandler<CambiarRolCommand, string>
{
    public async Task<Result<string>> Handle(CambiarRolCommand c, CancellationToken ct)
    {
        await repo.CambiarRol(user.UsuarioId, c.Request.Rol, c.Request.Predeterminado, user.Ip, ct);
        return Result.Success(c.Request.Rol);
    }
}
