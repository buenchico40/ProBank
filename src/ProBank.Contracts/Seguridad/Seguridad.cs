namespace ProBank.Contracts.Seguridad;

// Usuarios (SEG-01). El Token es el identificador opaco; el UsuarioId no sale de la API.
public sealed record UsuarioDto(Guid Token, string Usuario, string Nombre, string? Correo, IReadOnlyList<string> Roles,
    string? RolPredeterminado, string Estado, bool Bloqueado, bool DebeCambiarClave, DateTime? UltimaConexion, string? UltimaIp,
    string? IpPermitida);

public sealed record IngresoUsuarioDto(DateTime Fecha, string? Ip, string Resultado, string? Detalle);

public sealed record CrearUsuarioRequest(string Usuario, string Nombre, string? Correo, IReadOnlyList<string> Roles, string? RolPredeterminado);
public sealed record ActualizarUsuarioRequest(string Nombre, string? Correo, IReadOnlyList<string> Roles, string? RolPredeterminado, string Estado);

/// <summary>Clave temporal generada por la API: se muestra una sola vez y el usuario debe cambiarla al ingresar.</summary>
public sealed record ClaveTemporalDto(Guid Token, string Usuario, string ClaveTemporal);

// Roles y permisos (SEG-03)
public sealed record RolDto(int RolId, string Nombre, string? Descripcion, string Estado, int Opciones, int TotalOpciones, int Usuarios);
public sealed record GuardarRolRequest(string Nombre, string? Descripcion, string Estado);

/// <summary>Una fila de la matriz: opción del menú × acción genérica o función propia.</summary>
public sealed record PermisoDto(string Modulo, string Opcion, string OpcionNombre, string Accion, string AccionNombre, string Tipo, bool Concedido);

/// <summary>Permisos concedidos como "opcion:accion".</summary>
public sealed record GuardarPermisosRequest(IReadOnlyList<string> Permisos);

// Auditoría (SEG-05)
public sealed record AuditoriaDto(long AuditoriaId, DateTime Fecha, string Usuario, string? Nombre, string? RolActivo, string Modulo,
    string Accion, string? Detalle, string? Ip);

// Parámetros de acceso
public sealed record ParametroDto(string Clave, string Nombre, int Valor, int Minimo, int Maximo);
public sealed record GuardarParametrosRequest(IReadOnlyList<ValorParametro> Valores);
public sealed record ValorParametro(string Clave, int Valor);

// Perfil del usuario conectado
public sealed record PerfilDto(string Usuario, string Nombre, string? Correo, IReadOnlyList<string> Roles, string RolActivo,
    string? RolPredeterminado, bool DebeCambiarClave, DateTime? ClaveCambiada, DateTime? UltimaConexion, string? UltimaIp,
    DateTime? ConexionAnterior, string? IpAnterior, IReadOnlyList<string> Permisos, int UsuarioId);

public sealed record CambiarClaveRequest(string Actual, string Nueva);
public sealed record CambiarRolRequest(string Rol, bool Predeterminado);
