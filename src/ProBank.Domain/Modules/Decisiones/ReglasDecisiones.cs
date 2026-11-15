using ProBank.Domain.Common;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.Domain.Modules.Decisiones;

/// <summary>
/// Reglas de Decisiones (DEC-01 a DEC-08). La base valida permisos, estados, turnos, la huella y los sellos;
/// aquí se validan los datos que no dependen de ella.
/// </summary>
public static class ReglasDecisiones
{
    /// <summary>Texto de la descripción (sin etiquetas) y tamaño de su HTML con el formato del editor.</summary>
    public const int MaximoDescripcion = 8000;
    public const int MaximoDescripcionHtml = 100_000;
    public const int MaximoAprobadores = 20;

    /// <summary>DEC-08: archivos de respaldo; los tipos son los del catálogo de adjuntos (se verifica su contenido).</summary>
    public const int MaximoArchivos = 20;
    public const int MaximoMBArchivo = 20;
    public static IReadOnlyList<string> ExtensionesArchivo => ReglasAdjunto.Extensiones;

    public static Error? ValidarTipo(string? nombre, string? descripcion, IReadOnlyCollection<(int UsuarioId, int Orden)>? aprobadores)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100) return Error.Validation("nombre", "El nombre es obligatorio (máximo 100 caracteres).");
        if (descripcion?.Trim().Length > 300) return Error.Validation("descripcion", "La descripción admite hasta 300 caracteres.");
        if (aprobadores is null || aprobadores.Count == 0) return Error.Validation("aprobadores", "Agregue al menos un aprobador.");
        if (aprobadores.Count > MaximoAprobadores) return Error.Validation("aprobadores", $"Un tipo admite hasta {MaximoAprobadores} aprobadores.");
        if (aprobadores.Select(a => a.UsuarioId).Distinct().Count() != aprobadores.Count) return Error.Validation("aprobadores", "Un usuario aparece dos veces como aprobador.");
        if (aprobadores.Any(a => a.Orden is < 1 or > 99)) return Error.Validation("orden", "El orden va de 1 a 99.");
        return null;
    }

    /// <summary>El alcance de la decisión (montos incluidos) se describe en la descripción; aquí llega su texto sin etiquetas.</summary>
    public static Error? ValidarDecision(string? tipo, string? titulo, string? descripcion, string? area, int largoHtml = 0)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return Error.Validation("tipo", "Elija el tipo de decisión.");
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length > 200) return Error.Validation("titulo", "El título es obligatorio (máximo 200 caracteres).");
        if (string.IsNullOrWhiteSpace(descripcion)) return Error.Validation("descripcion", "Describa la decisión: qué se decide, por qué, con qué alcance y montos.");
        if (descripcion.Length > MaximoDescripcion || largoHtml > MaximoDescripcionHtml)
            return Error.Validation("descripcion", $"La descripción admite hasta {MaximoDescripcion} caracteres de texto.");
        if (string.IsNullOrWhiteSpace(area) || area.Trim().Length > 100) return Error.Validation("area", "El área es obligatoria (máximo 100 caracteres).");
        return null;
    }

    /// <summary>Al rechazar se explica el motivo; la clave siempre es obligatoria.</summary>
    public static Error? ValidarFirma(bool aprobar, string? clave, string? comentario)
    {
        if (string.IsNullOrEmpty(clave)) return Error.Validation("clave", "Ingrese su clave para firmar.");
        if (!aprobar && string.IsNullOrWhiteSpace(comentario)) return Error.Validation("comentario", "Indique el motivo del rechazo.");
        if (comentario?.Trim().Length > 500) return Error.Validation("comentario", "El comentario admite hasta 500 caracteres.");
        return null;
    }

    /// <summary>DEC-07: la versión nueva cumple las mismas reglas del contenido y lleva motivo y la clave de quien la modifica.</summary>
    public static Error? ValidarModificacion(string? titulo, string? descripcion, string? area, string? motivo, string? clave, int largoHtml = 0)
    {
        if (ValidarDecision("—", titulo, descripcion, area, largoHtml) is { } e) return e;
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 500) return Error.Validation("motivo", "Indique el motivo de la modificación (máximo 500 caracteres).");
        if (string.IsNullOrEmpty(clave)) return Error.Validation("clave", "Ingrese su clave para firmar la modificación.");
        return null;
    }

    public static Error? ValidarAnulacion(string? motivo) =>
        string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 500 ? Error.Validation("motivo", "Indique el motivo de la anulación (máximo 500 caracteres).") : null;
}
