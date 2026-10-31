namespace ProBank.Contracts.Seguridad;

/// <summary>La Web envía la IP del usuario: queda en auditoría y en el token (SEG-04).</summary>
public sealed record IngresarRequest(string Usuario, string Clave, string Ip);

/// <summary>Sesión emitida. <see cref="DebeCambiarClave"/>: la clave es temporal y la API solo permite el perfil hasta cambiarla (SEG-07).</summary>
public sealed record IngresoResponse(string Token, DateTimeOffset Expira, string Nombre, IReadOnlyList<string> Roles,
    string? RolPredeterminado, bool DebeCambiarClave);
