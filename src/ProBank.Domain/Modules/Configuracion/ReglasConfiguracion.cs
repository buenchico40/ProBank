using System.Globalization;
using System.Text.RegularExpressions;
using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.Configuracion;

/// <summary>Definición y valor de un parámetro global (cfg.Parametro).</summary>
public sealed record ParametroGlobal(string Clave, string Categoria, string Nombre, string Tipo, string? Opciones, int? Minimo, int? Maximo,
    string? Prefijo, string? Valor)
{
    public IReadOnlyList<string> ListaOpciones =>
        string.IsNullOrWhiteSpace(Opciones) ? [] : Opciones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>Reglas de la configuración global (CFG-01 a CFG-04) que no dependen de la base.</summary>
public static partial class ReglasConfiguracion
{
    // Claves que usa el código (la definición completa está en Scripts/Seed.PostDeployment.sql)
    public const string Proveedor = "Correo.Proveedor";
    public const string Buzon = "Correo.BuzonServicio";
    public const string SmtpServidor = "Correo.Smtp.Servidor";
    public const string SmtpPuerto = "Correo.Smtp.Puerto";
    public const string SmtpSeguridad = "Correo.Smtp.Seguridad";
    public const string SmtpUsuario = "Correo.Smtp.Usuario";
    public const string SmtpSecreto = "Correo.Smtp.SecretoClave";
    public const string SmtpRemitente = "Correo.Smtp.Remitente";
    public const string NombreRemitente = "Correo.NombreRemitente";
    public const string DiasConservacion = "Consultas.DiasConservacionResultados";
    public const string MaximoMBAdjunto = "Consultas.MaximoMBAdjunto";

    /// <summary>
    /// Valida el conjunto completo de valores (los guardados con los cambios aplicados): cada valor contra su tipo (número en rango,
    /// opción de la lista, correo, secreto con su prefijo) y las reglas del correo según el proveedor (CFG-03).
    /// <paramref name="permiteCarpeta"/>: solo en desarrollo se acepta el proveedor Carpeta.
    /// </summary>
    public static Error? Validar(IReadOnlyList<ParametroGlobal> definicion, IReadOnlyDictionary<string, string?> valores, bool permiteCarpeta)
    {
        foreach (var p in definicion)
        {
            var v = Valor(valores, p.Clave);
            if (v is null) continue;
            var error = p.Tipo switch
            {
                "Numero" when !int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < (p.Minimo ?? int.MinValue) || n > (p.Maximo ?? int.MaxValue)
                    => $"{p.Nombre}: un número entre {p.Minimo} y {p.Maximo}.",
                "Lista" when !p.ListaOpciones.Contains(v) => $"{p.Nombre}: elija {string.Join(", ", p.ListaOpciones)}.",
                "Correo" when !CorreoRegex().IsMatch(v) => $"{p.Nombre}: no es un correo válido.",
                "Secreto" when !SecretoRegex().IsMatch(v) || (p.Prefijo is not null && !v.StartsWith(p.Prefijo, StringComparison.Ordinal))
                    => $"{p.Nombre}: solo el nombre del secreto de Key Vault, con letras, números y guiones, que empiece con {p.Prefijo}.",
                _ when v.Length > 1000 => $"{p.Nombre}: máximo 1000 caracteres.",
                _ => null,
            };
            if (error is not null) return Error.Validation(p.Clave, error);
        }

        var proveedor = Valor(valores, Proveedor) ?? "Graph";
        if (proveedor == "Graph" && Valor(valores, Buzon) is null)
            return Error.Validation(Buzon, "Con Microsoft 365 (Graph), indique el buzón de servicio.");
        if (proveedor == "Smtp")
        {
            if (Valor(valores, SmtpServidor) is null) return Error.Validation(SmtpServidor, "Con SMTP, indique el servidor.");
            if (Valor(valores, SmtpRemitente) is null) return Error.Validation(SmtpRemitente, "Con SMTP, indique el remitente.");
            if (Valor(valores, SmtpUsuario) is not null && Valor(valores, SmtpSecreto) is null)
                return Error.Validation(SmtpSecreto, "Con usuario SMTP, indique el secreto de Key Vault con su clave.");
            if (Valor(valores, SmtpUsuario) is not null && Valor(valores, SmtpSeguridad) == "Ninguna")
                return Error.Validation(SmtpSeguridad, "No se envían usuario y clave sin cifrar: use StartTls o TlsDirecto.");
        }
        if (proveedor == "Carpeta" && !permiteCarpeta)
            return Error.Validation(Proveedor, "El proveedor Carpeta es solo para desarrollo.");
        return null;
    }

    /// <summary>Valor recortado; null si falta o está vacío.</summary>
    public static string? Valor(IReadOnlyDictionary<string, string?> valores, string clave) =>
        valores.TryGetValue(clave, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

    public static int Entero(IReadOnlyDictionary<string, string?> valores, string clave, int porDefecto) =>
        int.TryParse(Valor(valores, clave), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : porDefecto;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex CorreoRegex();

    [GeneratedRegex("^[A-Za-z0-9-]{1,127}$")]
    private static partial Regex SecretoRegex();
}
