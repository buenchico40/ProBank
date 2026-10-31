namespace ProBank.Application.Abstractions;

public interface ICurrentUser
{
    int UsuarioId { get; }
    /// <summary>Nombre de usuario con el que ingresó (claim preferred_username).</summary>
    string Usuario { get; }
    string Nombre { get; }
    string RolActivo { get; }
    string Ip { get; }
}

/// <summary>Quién hace el cambio: los SP de escritura validan su permiso y lo registran en auditoría.</summary>
public sealed record Actor(int UsuarioId, string RolActivo, string Ip)
{
    public static Actor De(ICurrentUser u) => new(u.UsuarioId, u.RolActivo, u.Ip);
}

/// <summary>Credencial guardada de un usuario (seg.usp_Usuario_ObtenerCredencial).</summary>
public sealed record CredencialUsuario(int UsuarioId, string Usuario, string Nombre, byte[]? ClaveHash, byte[]? ClaveSal,
    string? IpPermitida, string Estado, bool Bloqueado, IReadOnlyList<string> Roles,
    string? RolPredeterminado = null, bool DebeCambiarClave = false, int SelloSesion = 1, int MinutosSesion = 60);

public interface ICredencialesRepository
{
    Task<CredencialUsuario?> Obtener(string usuario, CancellationToken ct);
    Task RegistrarIngreso(string usuario, bool exitoso, string? motivo, string ip, CancellationToken ct);
}

/// <summary>SEG-03: opción × acción del rol activo del usuario actual (seg.fn_TienePermiso).</summary>
public interface IPermisos
{
    Task<bool> Tiene(string opcion, string accion, CancellationToken ct);
}

/// <summary>Estado vigente de las sesiones de un usuario: sello y estado de la cuenta (SEG-07).</summary>
public sealed record SesionVigente(int Sello, string Estado);

/// <summary>
/// Consulta el sello de sesión con caché corta. <see cref="Invalidar"/> se llama tras revocar o cambiar la clave para que
/// esta instancia de la API lo note de inmediato; las demás lo notan al vencer la caché.
/// </summary>
public interface ISesiones
{
    Task<SesionVigente?> Obtener(int usuarioId, CancellationToken ct);
    void Invalidar(int usuarioId);
}

public interface IClock
{
    DateTime Ahora { get; }
    DateOnly Hoy { get; }
}

public interface IAuditLog
{
    Task Registrar(string modulo, string accion, string detalle, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task Ejecutar(Func<CancellationToken, Task> trabajo, CancellationToken ct);
}

public interface IMailSender
{
    Task Enviar(IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml,
        (string Nombre, byte[] Contenido, string Tipo)? adjunto, CancellationToken ct);
}
