using System.Text.RegularExpressions;
using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.ConsultasDinamicas;

public enum TipoParametro { Texto, Numero, Fecha, Lista }

/// <summary>Parámetro configurado; <see cref="Opciones"/> separadas por coma cuando es una lista.</summary>
public sealed record ParametroConsulta(string Nombre, string Etiqueta, TipoParametro Tipo, bool Obligatorio, string? ValorDefecto, string? Opciones = null)
{
    public IReadOnlyList<string> ListaOpciones =>
        string.IsNullOrWhiteSpace(Opciones) ? [] : Opciones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed partial class Consulta
{
    public int ConsultaId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Procedimiento { get; init; } = string.Empty;
    public int TiempoMaximoSeg { get; init; } = 30;
    public int LimiteFilas { get; init; } = 10_000;
    public bool Publicada { get; init; }
    /// <summary>Códigos de las bases activas permitidas (CNX-001).</summary>
    public IReadOnlyList<string> ConexionesPermitidas { get; init; } = [];
    public IReadOnlyList<string> RolesPermitidos { get; init; } = [];
    public IReadOnlyList<int> Aprobadores { get; init; } = [];
    public IReadOnlyList<ParametroConsulta> Parametros { get; init; } = [];

    /// <summary>SQL-01: solo un nombre de procedimiento de dos partes (esquema.nombre); nunca texto SQL.</summary>
    public static bool EsProcedimientoValido(string? nombre) => nombre is not null && ProcedimientoRegex().IsMatch(nombre);

    /// <summary>El Administrador ejecuta cualquier consulta publicada; los demás roles, las que tienen asignadas.</summary>
    public bool RolPuedeEjecutar(string rolActivo) => rolActivo == "Administrador" || RolesPermitidos.Contains(rolActivo);

    public Result PuedeEjecutar(string rolActivo, string conexion)
    {
        if (!Publicada) return Result.Failure(Error.Validation("no_publicada", "Solo se ejecutan consultas publicadas."));
        if (!RolPuedeEjecutar(rolActivo))
            return Result.Failure(Error.Forbidden($"El rol {rolActivo} no puede ejecutar {Codigo}."));
        if (!ConexionesPermitidas.Contains(conexion))
            return Result.Failure(Error.Validation("conexion", "La base de datos no está permitida para esta consulta o está inactiva."));
        if (!EsProcedimientoValido(Procedimiento))
            return Result.Failure(Error.Validation("procedimiento", "Procedimiento no válido."));
        return Result.Success();
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]{0,127}\.[A-Za-z_][A-Za-z0-9_]{0,127}$")]
    private static partial Regex ProcedimientoRegex();
}
