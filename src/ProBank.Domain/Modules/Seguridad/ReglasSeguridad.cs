using System.Text.RegularExpressions;
using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Seguridad;

/// <summary>Reglas de usuarios y roles que no dependen de la base (la base vuelve a validar permisos y unicidad).</summary>
public static partial class ReglasSeguridad
{
    public const string RolAdministrador = "Administrador";
    public static readonly IReadOnlyList<string> Estados = ["Activo", "Inactivo"];

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]{2,59}$")]
    private static partial Regex FormatoUsuario();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatoCorreo();

    /// <summary>Usuario en minúsculas, de 3 a 60 caracteres: letras, números, punto, guion y guion bajo.</summary>
    public static Error? ValidarUsuario(string usuario) => FormatoUsuario().IsMatch(usuario)
        ? null
        : Error.Validation("usuario", "El usuario debe tener de 3 a 60 caracteres en minúsculas: letras, números, punto, guion o guion bajo.");

    /// <summary>SEG-01: varios roles y uno predeterminado que debe estar entre los asignados.</summary>
    public static Error? ValidarDatos(string nombre, string? correo, IReadOnlyList<string> roles, string? rolPredeterminado, string estado)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150) return Error.Validation("nombre", "El nombre es obligatorio (máximo 150 caracteres).");
        if (!string.IsNullOrWhiteSpace(correo) && (correo.Length > 256 || !FormatoCorreo().IsMatch(correo)))
            return Error.Validation("correo", "El correo no tiene un formato válido.");
        if (roles.Count == 0) return Error.Validation("roles", "Asigna al menos un rol.");
        if (rolPredeterminado is not null && !roles.Contains(rolPredeterminado))
            return Error.Validation("rol_predeterminado", "El rol predeterminado debe estar entre los roles asignados.");
        if (!Estados.Contains(estado)) return Error.Validation("estado", "El estado debe ser Activo o Inactivo.");
        return null;
    }

    public static Error? ValidarRol(string nombre, string? descripcion, string estado)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 60) return Error.Validation("nombre", "El nombre del rol es obligatorio (máximo 60 caracteres).");
        if (descripcion is { Length: > 300 }) return Error.Validation("descripcion", "La descripción admite hasta 300 caracteres.");
        if (!Estados.Contains(estado)) return Error.Validation("estado", "El estado debe ser Activo o Inactivo.");
        return null;
    }
}
