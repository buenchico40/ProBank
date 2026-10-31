using System.Text.RegularExpressions;
using ProBank.Domain.Common;

namespace ProBank.Domain.Modules.ConsultasDinamicas;

public enum TipoParametro { Texto, Numero, Fecha, Lista }

public sealed record ParametroConsulta(string Nombre, string Etiqueta, TipoParametro Tipo, bool Obligatorio, string? ValorDefecto);

public sealed partial class Consulta
{
    public int ConsultaId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Procedimiento { get; init; } = string.Empty;
    public int TiempoMaximoSeg { get; init; } = 30;
    public int LimiteFilas { get; init; } = 10_000;
    public bool Publicada { get; init; }
    public IReadOnlyList<int> ConexionesPermitidas { get; init; } = [];
    public IReadOnlyList<string> RolesPermitidos { get; init; } = [];
    public IReadOnlyList<int> Aprobadores { get; init; } = [];
    public IReadOnlyList<ParametroConsulta> Parametros { get; init; } = [];

    /// <summary>SQL-01: solo un nombre de procedimiento de dos partes (esquema.nombre); nunca texto SQL.</summary>
    public static bool EsProcedimientoValido(string nombre) => ProcedimientoRegex().IsMatch(nombre);

    public Result PuedeEjecutar(string rolActivo, int conexionId)
    {
        if (!Publicada) return Result.Failure(Error.Validation("no_publicada", "Solo se ejecutan consultas publicadas."));
        if (rolActivo != "Administrador" && !RolesPermitidos.Contains(rolActivo))
            return Result.Failure(Error.Forbidden($"El rol {rolActivo} no puede ejecutar {Codigo}."));
        if (!ConexionesPermitidas.Contains(conexionId))
            return Result.Failure(Error.Validation("conexion", "La base de datos no está permitida para esta consulta."));
        if (!EsProcedimientoValido(Procedimiento))
            return Result.Failure(Error.Validation("procedimiento", "Procedimiento no válido."));
        return Result.Success();
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]{0,127}\.[A-Za-z_][A-Za-z0-9_]{0,127}$")]
    private static partial Regex ProcedimientoRegex();
}
