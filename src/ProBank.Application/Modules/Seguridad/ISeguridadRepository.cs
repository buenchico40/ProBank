using ProBank.Application.Abstractions;
using ProBank.Contracts.Seguridad;

namespace ProBank.Application.Modules.Seguridad;

public interface ISeguridadRepository
{
    // Usuarios
    Task<IReadOnlyList<UsuarioDto>> ListarUsuarios(CancellationToken ct);
    Task<IReadOnlyList<IngresoUsuarioDto>> ListarIngresos(Guid token, CancellationToken ct);
    Task<Guid> CrearUsuario(CrearUsuarioRequest r, byte[] hash, byte[] sal, Actor actor, CancellationToken ct);
    /// <returns>UsuarioId del usuario actualizado (para invalidar su sesión).</returns>
    Task<int> ActualizarUsuario(Guid token, ActualizarUsuarioRequest r, Actor actor, CancellationToken ct);
    Task<int> RestablecerClave(Guid token, byte[] hash, byte[] sal, Actor actor, CancellationToken ct);
    Task Desbloquear(Guid token, Actor actor, CancellationToken ct);
    Task<int> RevocarSesiones(Guid token, Actor actor, CancellationToken ct);

    // Perfil
    Task<PerfilDto?> ObtenerPerfil(int usuarioId, string rolActivo, CancellationToken ct);
    Task CambiarClave(int usuarioId, byte[] hash, byte[] sal, string rolActivo, string ip, CancellationToken ct);
    Task CambiarRol(int usuarioId, string rol, bool predeterminado, string ip, CancellationToken ct);
    Task<SesionVigente?> ObtenerSello(int usuarioId, CancellationToken ct);

    // Roles y permisos
    Task<IReadOnlyList<RolDto>> ListarRoles(CancellationToken ct);
    Task<int> GuardarRol(int? rolId, GuardarRolRequest r, Actor actor, CancellationToken ct);
    Task<IReadOnlyList<PermisoDto>> ListarPermisos(int rolId, CancellationToken ct);
    Task GuardarPermisos(int rolId, IReadOnlyList<string> permisos, Actor actor, CancellationToken ct);

    // Auditoría y parámetros
    Task<IReadOnlyList<AuditoriaDto>> ListarAuditoria(DateOnly desde, DateOnly hasta, string? modulo, CancellationToken ct);
    Task<IReadOnlyList<ParametroDto>> ListarParametros(CancellationToken ct);
    Task GuardarParametros(IReadOnlyList<ValorParametro> valores, Actor actor, CancellationToken ct);
}
