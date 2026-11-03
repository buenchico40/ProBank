using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Portafolio;

/// <summary>Dueño de un documento adjunto: un proyecto, mejora o bug, o una tarea.</summary>
public enum AmbitoAdjunto { Item, Tarea }

/// <summary>
/// Documentos adjuntos de Portafolio. Cada ámbito tiene sus tipos y su tamaño máximo (pry.ParametroAdjunto),
/// elegidos de este catálogo cerrado: solo tipos cuyo contenido se puede verificar.
/// </summary>
public static class ReglasAdjunto
{
    public const int MaximoMBPermitido = 20;
    public const int MaximoPorDueno = 20;

    private enum Firma { Pdf, Zip, Ole, Png, Jpg, Texto }
    private sealed record TipoArchivo(string Contenido, Firma Firma);

    /// <summary>Extensión → tipo de contenido y firma del archivo.</summary>
    private static readonly Dictionary<string, TipoArchivo> Catalogo = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new("application/pdf", Firma.Pdf),
        [".doc"] = new("application/msword", Firma.Ole),
        [".docx"] = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", Firma.Zip),
        [".xls"] = new("application/vnd.ms-excel", Firma.Ole),
        [".xlsx"] = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Firma.Zip),
        [".ppt"] = new("application/vnd.ms-powerpoint", Firma.Ole),
        [".pptx"] = new("application/vnd.openxmlformats-officedocument.presentationml.presentation", Firma.Zip),
        [".zip"] = new("application/zip", Firma.Zip),
        [".sql"] = new("text/plain", Firma.Texto),
        [".txt"] = new("text/plain", Firma.Texto),
        [".csv"] = new("text/csv", Firma.Texto),
        [".xml"] = new("application/xml", Firma.Texto),
        [".png"] = new("image/png", Firma.Png),
        [".jpg"] = new("image/jpeg", Firma.Jpg),
        [".jpeg"] = new("image/jpeg", Firma.Jpg),
    };

    /// <summary>Extensiones que se pueden habilitar en la parametrización.</summary>
    public static IReadOnlyList<string> Extensiones { get; } = [.. Catalogo.Keys];

    /// <summary>Bytes del inicio del archivo que hacen falta para verificar su contenido.</summary>
    public const int BytesCabecera = 512;

    /// <summary>
    /// Valida nombre, tipo habilitado, tamaño y que el contenido corresponda a la extensión (no basta con renombrar un archivo).
    /// Devuelve el nombre limpio y el tipo de contenido, o el error.
    /// </summary>
    public static (string Nombre, string Tipo, Error? Error) Validar(string? nombre, long tamano, ReadOnlySpan<byte> cabecera,
        IReadOnlyCollection<string> permitidas, int maximoMB)
    {
        var limpio = Path.GetFileName((nombre ?? "").Replace('\\', '/')).Trim();
        if (limpio.Length is 0 or > 200) return ("", "", Error.Validation("nombre", "El nombre del archivo es obligatorio (máximo 200 caracteres)."));
        var ext = Path.GetExtension(limpio);
        if (!Catalogo.TryGetValue(ext, out var tipo) || !permitidas.Contains(ext, StringComparer.OrdinalIgnoreCase))
            return ("", "", Error.Validation("tipo", $"Tipo de archivo no permitido. Se aceptan: {string.Join(", ", permitidas)}."));
        if (tamano <= 0) return ("", "", Error.Validation("tamano", "El archivo está vacío."));
        if (tamano > maximoMB * 1024L * 1024L) return ("", "", Error.Validation("tamano", $"El archivo supera el máximo de {maximoMB} MB."));
        if (!Corresponde(tipo.Firma, cabecera)) return ("", "", Error.Validation("contenido", "El contenido del archivo no corresponde a su extensión."));
        return (limpio, tipo.Contenido, null);
    }

    private static bool Corresponde(Firma firma, ReadOnlySpan<byte> c) => firma switch
    {
        Firma.Pdf => c.StartsWith("%PDF"u8),
        Firma.Zip => c.StartsWith((ReadOnlySpan<byte>)[0x50, 0x4B, 0x03, 0x04]),
        Firma.Ole => c.StartsWith((ReadOnlySpan<byte>)[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]),
        Firma.Png => c.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47]),
        Firma.Jpg => c.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]),
        // Texto (.sql, .txt, .csv): sin bytes nulos ni cabecera de ejecutable
        _ => c.IndexOf((byte)0) < 0 && !c.StartsWith("MZ"u8),
    };

    /// <summary>Cada documento lleva una descripción (qué es y para qué sirve). Devuelve la descripción limpia o el error.</summary>
    public static (string Descripcion, Error? Error) ValidarDescripcion(string? descripcion)
    {
        var d = (descripcion ?? "").Trim();
        return d.Length is 0 or > 300
            ? ("", Error.Validation("descripcion", "Describa el documento (máximo 300 caracteres)."))
            : (d, null);
    }

    /// <summary>Valida la parametrización de un ámbito; devuelve las extensiones normalizadas (".pdf,.docx") o el error.</summary>
    public static (string? Extensiones, Error? Error) ValidarParametro(IReadOnlyCollection<string>? extensiones, int maximoMB)
    {
        var lista = (extensiones ?? []).Select(e => e.Trim().ToLowerInvariant()).Where(e => e.Length > 0)
            .Select(e => e.StartsWith('.') ? e : "." + e).Distinct().ToList();
        if (lista.Count == 0) return (null, Error.Validation("extensiones", "Seleccione al menos un tipo de archivo."));
        var malas = lista.Where(e => !Catalogo.ContainsKey(e)).ToList();
        if (malas.Count > 0) return (null, Error.Validation("extensiones", $"Tipo de archivo no admitido: {string.Join(", ", malas)}."));
        if (maximoMB is < 1 or > MaximoMBPermitido) return (null, Error.Validation("maximo", $"El tamaño máximo va de 1 a {MaximoMBPermitido} MB."));
        return (string.Join(",", lista), null);
    }
}
