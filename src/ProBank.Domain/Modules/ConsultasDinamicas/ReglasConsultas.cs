using System.Text.RegularExpressions;
using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.ConsultasDinamicas;

/// <summary>Reglas de Consultas dinámicas (SQL-01 a SQL-08) que no dependen de la base.</summary>
public static partial class ReglasConsultas
{
    public static readonly IReadOnlyList<string> Autenticaciones = ["Usuario SQL de solo lectura", "Identidad administrada (Entra ID)"];
    /// <summary>SQL-05: formatos de exportación (y de envío programado).</summary>
    public static readonly IReadOnlyList<string> Formatos = ["xlsx", "csv", "pdf", "json", "txt"];
    public static readonly IReadOnlyList<string> Frecuencias = ["Diaria", "Semanal", "Mensual"];

    /// <summary>Control del parámetro según su tipo SQL (sys.parameters): fechas, números o texto.</summary>
    public static TipoParametro TipoDesdeSql(string tipoSql) => tipoSql.ToLowerInvariant() switch
    {
        "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => TipoParametro.Fecha,
        "int" or "bigint" or "smallint" or "tinyint" or "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => TipoParametro.Numero,
        _ => TipoParametro.Texto,
    };

    public static Error? ValidarConexion(string? nombre, string? servidor, string? baseDatos, string? autenticacion, string? secreto, string? estado)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 150) return Error.Validation("nombre", "El nombre es obligatorio (máximo 150 caracteres).");
        if (string.IsNullOrWhiteSpace(servidor) || servidor.Trim().Length > 200) return Error.Validation("servidor", "El servidor es obligatorio (máximo 200 caracteres).");
        if (string.IsNullOrWhiteSpace(baseDatos) || baseDatos.Trim().Length > 128) return Error.Validation("baseDatos", "La base de datos es obligatoria (máximo 128 caracteres).");
        if (autenticacion is null || !Autenticaciones.Contains(autenticacion)) return Error.Validation("autenticacion", "Elija la autenticación.");
        if (secreto is null || !SecretoRegex().IsMatch(secreto))
            return Error.Validation("secreto", "El nombre del secreto de Key Vault empieza con cnx- y tiene solo letras, números o guiones (por ejemplo cnx-core-lectura).");
        if (estado is not ("Activo" or "Inactivo")) return Error.Validation("estado", "El estado es Activo o Inactivo.");
        return null;
    }

    public static Error? ValidarConsulta(string? nombre, string? procedimiento, int tiempoMaximoSeg, int limiteFilas, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 150) return Error.Validation("nombre", "El nombre es obligatorio (máximo 150 caracteres).");
        if (!Consulta.EsProcedimientoValido(procedimiento))
            return Error.Validation("procedimiento", "Indique un procedimiento almacenado como esquema.nombre (no se aceptan consultas escritas a mano).");
        if (tiempoMaximoSeg is < 5 or > 600) return Error.Validation("tiempo", "El tiempo máximo va de 5 a 600 segundos.");
        if (limiteFilas is < 1 or > 100_000) return Error.Validation("limite", "El límite de filas va de 1 a 100.000.");
        if (descripcion?.Trim().Length > 500) return Error.Validation("descripcion", "La descripción admite hasta 500 caracteres.");
        return null;
    }

    /// <summary>
    /// SQL-04: etiqueta obligatoria; una lista necesita opciones; el valor por defecto de una fecha es una fecha o una variable de
    /// fecha (Hoy, Fin de mes…); el de un número, un número o una variable numérica (Año actual…); el de una lista, una de sus opciones.
    /// </summary>
    public static Error? ValidarParametro(ParametroConsulta p)
    {
        if (string.IsNullOrWhiteSpace(p.Etiqueta) || p.Etiqueta.Trim().Length > 100) return Error.Validation("etiqueta", $"La etiqueta de {p.Nombre} es obligatoria (máximo 100 caracteres).");
        if (p.Tipo == TipoParametro.Lista && p.ListaOpciones.Count == 0) return Error.Validation("opciones", $"{p.Etiqueta}: una lista necesita sus opciones separadas por coma.");
        var d = p.ValorDefecto?.Trim();
        if (string.IsNullOrEmpty(d)) return null;
        var ok = p.Tipo switch
        {
            TipoParametro.Fecha => VariablesParametro.Fechas.ContainsKey(d) || DateOnly.TryParseExact(d, "dd/MM/yyyy", out _),
            TipoParametro.Numero => VariablesParametro.Numeros.ContainsKey(d) || decimal.TryParse(d, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _),
            TipoParametro.Lista => p.ListaOpciones.Contains(d),
            _ => d.Length <= 200,
        };
        return ok ? null : Error.Validation("valorDefecto", $"{p.Etiqueta}: el valor por defecto no corresponde al tipo {p.Tipo.ToString().ToLowerInvariant()}.");
    }

    public static Error? ValidarProgramacion(string? frecuencia, IReadOnlyCollection<int> dias, int? diaDelMes, string? formato, IReadOnlyCollection<string> destinatarios)
    {
        if (frecuencia is null || !Frecuencias.Contains(frecuencia)) return Error.Validation("frecuencia", "La frecuencia es Diaria, Semanal o Mensual.");
        if (frecuencia == "Semanal" && (dias.Count == 0 || dias.Any(d => d is < 0 or > 6))) return Error.Validation("dias", "Elija los días de la semana.");
        if (frecuencia == "Mensual" && diaDelMes is < 0 or > 31) return Error.Validation("diaDelMes", "El día del mes va de 1 a 31 (0 = último día).");
        if (formato is null || !Formatos.Contains(formato)) return Error.Validation("formato", "El formato es Excel, CSV, PDF, JSON o texto.");
        if (destinatarios.Count == 0) return Error.Validation("destinatarios", "Indique al menos un destinatario.");
        if (destinatarios.FirstOrDefault(c => !CorreoRegex().IsMatch(c)) is { } malo) return Error.Validation("destinatarios", $"El correo {malo} no es válido.");
        return null;
    }

    // CFG-02: solo secretos de conexiones (cnx-…): una base no puede usar como cadena otro secreto de Key Vault
    [GeneratedRegex("^cnx-[A-Za-z0-9-]{1,123}$")]
    private static partial Regex SecretoRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex CorreoRegex();
}
