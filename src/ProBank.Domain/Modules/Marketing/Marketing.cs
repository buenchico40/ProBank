using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Marketing;

/// <summary>
/// Reglas de Marketing (MKT-01 y MKT-02). La base valida permisos, audiencia (rol activo) y las transiciones de estado;
/// aquí se validan los datos que no dependen de ella.
/// </summary>
public static class ReglasMarketing
{
    public static readonly IReadOnlyList<string> Ubicaciones = ["Panel principal", "Inicio de sesión"];
    public static readonly IReadOnlyList<string> TiposComunicado = ["Aviso", "Noticia", "Mantenimiento", "Campaña"];

    /// <summary>Imagen del carrusel: JPG o PNG (se verifica el contenido), de 1200 × 300 px recomendados y hasta 2 MB.</summary>
    public static readonly IReadOnlyList<string> ExtensionesImagen = [".jpg", ".jpeg", ".png"];
    public const int MaximoMBImagen = 2;

    /// <summary>
    /// Estado que se muestra (igual que mkt.fn_EstadoVigente): lo publicado es Programado antes de su vigencia,
    /// Publicado durante ella y Vencido después; Borrador y Retirado no cambian.
    /// </summary>
    public static string EstadoVigente(string estado, DateOnly? desde, DateOnly? hasta, DateOnly hoy) =>
        estado != "Publicado" ? estado : desde > hoy ? "Programado" : hasta < hoy ? "Vencido" : "Publicado";

    public static Error? ValidarPromocion(string? titulo, string? ubicacion, DateOnly? desde, DateOnly? hasta, int orden, string? audiencia,
        string? textoAlternativo, string? enlace)
    {
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length > 200) return Error.Validation("titulo", "El título es obligatorio (máximo 200 caracteres).");
        if (ubicacion is null || !Ubicaciones.Contains(ubicacion)) return Error.Validation("ubicacion", "La ubicación es Panel principal o Inicio de sesión.");
        if (orden is < 1 or > 99) return Error.Validation("orden", "El orden va de 1 a 99.");
        if (textoAlternativo?.Trim().Length > 250) return Error.Validation("texto", "El texto alternativo admite hasta 250 caracteres.");
        if (EnlaceInvalido(enlace)) return Error.Validation("enlace", "El enlace debe ser una dirección https:// válida (máximo 400 caracteres).");
        return ValidarComun(desde, hasta, audiencia);
    }

    public static Error? ValidarComunicado(string? titulo, string? tipo, string? resumen, string? contacto, DateOnly? desde, DateOnly? hasta, string? audiencia)
    {
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length > 200) return Error.Validation("titulo", "El título es obligatorio (máximo 200 caracteres).");
        if (tipo is null || !TiposComunicado.Contains(tipo)) return Error.Validation("tipo", "El tipo es Aviso, Noticia, Mantenimiento o Campaña.");
        if (string.IsNullOrWhiteSpace(resumen) || resumen.Trim().Length > 300) return Error.Validation("resumen", "El resumen es obligatorio (máximo 300 caracteres).");
        if (contacto?.Trim().Length > 150) return Error.Validation("contacto", "El contacto admite hasta 150 caracteres.");
        return ValidarComun(desde, hasta, audiencia);
    }

    private static Error? ValidarComun(DateOnly? desde, DateOnly? hasta, string? audiencia)
    {
        if (desde is not null && hasta is not null && hasta < desde) return Error.Validation("vigencia", "La fecha final no puede ser anterior a la inicial.");
        if (string.IsNullOrWhiteSpace(audiencia) || audiencia.Length > 60) return Error.Validation("audiencia", "Elija la audiencia: Todos o un rol.");
        return null;
    }

    // Solo https absoluto: el enlace se abre desde el carrusel del Panel o del inicio de sesión
    private static bool EnlaceInvalido(string? enlace) =>
        !string.IsNullOrWhiteSpace(enlace)
        && (enlace.Trim().Length > 400 || !Uri.TryCreate(enlace.Trim(), UriKind.Absolute, out var u) || u.Scheme != Uri.UriSchemeHttps);
}
