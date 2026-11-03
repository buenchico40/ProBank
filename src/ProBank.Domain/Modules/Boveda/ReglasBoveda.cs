using System.Text.RegularExpressions;
using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Boveda;

/// <summary>
/// Reglas de la Bóveda (CRE-01 a CRE-03). La base valida permisos, la pertenencia al grupo de acceso y los duplicados;
/// aquí se validan los datos que no dependen de ella.
/// </summary>
public static partial class ReglasBoveda
{
    public const string IdentidadAdministrada = "Identidad administrada de Azure";
    public static readonly IReadOnlyList<string> TiposCertificado = ["SSL/TLS", "Firma de código", "Cliente", "Firma SAML"];
    public static readonly IReadOnlyList<string> TiposCuenta =
        ["Cuenta de servicio de Entra ID", "Cuenta de servicio del AD (on-premise)", "Cuenta de base de datos", IdentidadAdministrada];
    public static readonly IReadOnlyList<string> TiposSecreto = ["Cadena de conexión", "Clave de API", "Secreto de cliente (Entra ID)", "Token SAS"];

    /// <summary>Días de aviso antes del vencimiento que se ofrecen (la base acepta de 1 a 365).</summary>
    public static readonly IReadOnlyList<int> AvisoCertificado = [15, 30, 45, 60, 90, 120];
    public static readonly IReadOnlyList<int> AvisoSecreto = [7, 15, 30, 45, 60];
    /// <summary>Próxima rotación (cuenta) o vigencia (secreto) al cambiar el valor.</summary>
    public static readonly IReadOnlyList<int> DiasVigencia = [30, 60, 90, 180, 365];

    /// <summary>Una cuenta técnica pasa a «Por rotar» estos días antes de su próxima rotación.</summary>
    public const int DiasPorRotar = 15;
    public const int LongitudMinimaValor = 16;
    public const int LongitudMaximaValor = 4000;

    /// <summary>Igual que cred.fn_EstadoVence: Inactivo, Vencido, Por vencer (dentro del aviso) o Vigente.</summary>
    public static string EstadoVence(bool activo, DateOnly? vence, int diasAviso, DateOnly hoy) =>
        !activo ? "Inactivo" : vence is null ? "Vigente" : vence < hoy ? "Vencido" : vence.Value.DayNumber - hoy.DayNumber <= diasAviso ? "Por vencer" : "Vigente";

    /// <summary>Igual que cred.fn_EstadoCuenta: Inactivo, Rotación vencida, Por rotar (15 días antes) o Activo; sin rotación, Activo.</summary>
    public static string EstadoCuenta(bool activo, DateOnly? proxima, DateOnly hoy) =>
        !activo ? "Inactivo" : proxima is null ? "Activo" : proxima < hoy ? "Rotación vencida"
            : proxima.Value.DayNumber - hoy.DayNumber <= DiasPorRotar ? "Por rotar" : "Activo";

    /// <summary>Huella SHA-1 (40) o SHA-256 (64) en hexadecimal; se aceptan espacios y dos puntos, y se guarda en mayúsculas.</summary>
    public static string? NormalizarHuella(string? huella)
    {
        var h = (huella ?? "").Replace(" ", "").Replace(":", "").Trim().ToUpperInvariant();
        return h.Length == 0 ? null : h;
    }

    public static bool HuellaValida(string? huella) => huella is not null && Hex().IsMatch(huella) && huella.Length is 40 or 64;

    public static Error? ValidarGrupo(string? nombre, string? descripcion, string? estado, IReadOnlyCollection<int>? miembros)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100) return Error.Validation("nombre", "El nombre es obligatorio (máximo 100 caracteres).");
        if (descripcion?.Trim().Length > 300) return Error.Validation("descripcion", "La descripción admite hasta 300 caracteres.");
        if (estado is not ("Activo" or "Inactivo")) return Error.Validation("estado", "El estado es Activo o Inactivo.");
        if (miembros is null || miembros.Count == 0) return Error.Validation("miembros", "Agregue al menos un miembro: sin miembros nadie podría ver sus credenciales.");
        return null;
    }

    public static Error? ValidarCertificado(string? nombreComun, string? tipo, string? emisor, string? almacen, string? responsable, int diasAviso,
        string? huella, string? nombresAlternativos, string? usadoPor)
    {
        if (string.IsNullOrWhiteSpace(nombreComun) || nombreComun.Trim().Length > 200) return Error.Validation("nombre", "El nombre común (CN) es obligatorio (máximo 200 caracteres).");
        if (tipo is null || !TiposCertificado.Contains(tipo)) return Error.Validation("tipo", "El tipo es SSL/TLS, Firma de código, Cliente o Firma SAML.");
        if (Texto(emisor, 150) is null) return Error.Validation("emisor", "El emisor es obligatorio (máximo 150 caracteres).");
        if (Texto(almacen, 150) is null) return Error.Validation("almacen", "Indique dónde está almacenado (máximo 150 caracteres).");
        if (Texto(responsable, 100) is null) return Error.Validation("responsable", "El responsable es obligatorio (máximo 100 caracteres).");
        if (diasAviso is < 1 or > 365) return Error.Validation("aviso", "Los días de aviso van de 1 a 365.");
        if (huella is not null && !HuellaValida(huella)) return Error.Validation("huella", "La huella digital es SHA-1 (40) o SHA-256 (64) en hexadecimal.");
        if (nombresAlternativos?.Trim().Length > 500) return Error.Validation("san", "Los nombres alternativos admiten hasta 500 caracteres.");
        if (usadoPor?.Trim().Length > 200) return Error.Validation("uso", "«Usado por» admite hasta 200 caracteres.");
        return null;
    }

    public static Error? ValidarRenovacion(DateOnly vence, string? huella, DateOnly hoy)
    {
        if (vence <= hoy) return Error.Validation("vence", "El nuevo vencimiento debe ser posterior a hoy.");
        if (!HuellaValida(huella)) return Error.Validation("huella", "Indique la huella digital del nuevo certificado: SHA-1 (40) o SHA-256 (64) en hexadecimal.");
        return null;
    }

    public static Error? ValidarCuenta(string? nombre, string? tipo, string? sistema, string? responsable, string? privilegios, Guid? grupo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 150) return Error.Validation("nombre", "El nombre de la cuenta es obligatorio (máximo 150 caracteres).");
        if (tipo is null || !TiposCuenta.Contains(tipo)) return Error.Validation("tipo", "Elija el tipo de cuenta técnica.");
        if (Texto(sistema, 150) is null) return Error.Validation("sistema", "El sistema es obligatorio (máximo 150 caracteres).");
        if (Texto(responsable, 100) is null) return Error.Validation("responsable", "El responsable es obligatorio (máximo 100 caracteres).");
        if (privilegios?.Trim().Length > 300) return Error.Validation("privilegios", "Los privilegios admiten hasta 300 caracteres.");
        if (tipo != IdentidadAdministrada && grupo is null) return Error.Validation("grupo", "Elija el grupo de acceso: solo sus miembros podrán ver la contraseña.");
        return null;
    }

    public static Error? ValidarSecreto(string? nombre, string? tipo, string? almacen, string? sistema, int diasAviso)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 150) return Error.Validation("nombre", "El nombre del secreto es obligatorio (máximo 150 caracteres).");
        if (tipo is null || !TiposSecreto.Contains(tipo)) return Error.Validation("tipo", "Elija el tipo de secreto.");
        if (Texto(almacen, 100) is null) return Error.Validation("almacen", "El almacén es obligatorio (máximo 100 caracteres).");
        if (Texto(sistema, 150) is null) return Error.Validation("sistema", "El sistema es obligatorio (máximo 150 caracteres).");
        if (diasAviso is < 1 or > 365) return Error.Validation("aviso", "Los días de aviso van de 1 a 365.");
        return null;
    }

    /// <summary>El valor no se recorta ni se transforma: se guarda exactamente como se escribió.</summary>
    public static Error? ValidarValor(string? valor, int dias)
    {
        if (string.IsNullOrEmpty(valor) || valor.Length < LongitudMinimaValor) return Error.Validation("valor", $"Use al menos {LongitudMinimaValor} caracteres.");
        if (valor.Length > LongitudMaximaValor) return Error.Validation("valor", $"El valor admite hasta {LongitudMaximaValor} caracteres.");
        if (!DiasVigencia.Contains(dias)) return Error.Validation("dias", "Elija la vigencia: 30, 60, 90, 180 o 365 días.");
        return null;
    }

    private static string? Texto(string? s, int max) => string.IsNullOrWhiteSpace(s) || s.Trim().Length > max ? null : s.Trim();

    [GeneratedRegex("^[0-9A-F]+$")]
    private static partial Regex Hex();
}
