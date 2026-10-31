using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.Application.Modules.ConsultasDinamicas;

public sealed record ResultadoEjecucion(IReadOnlyList<string> Columnas, IReadOnlyList<IReadOnlyDictionary<string, object?>> Filas, int DuracionMs, bool Truncado);

public interface IConsultaRepository
{
    Task<Consulta?> Obtener(string codigo, CancellationToken ct);
    Task<(long Id, string Codigo)> CrearEjecucion(int consultaId, int conexionId, int solicitanteId, IReadOnlyList<int> aprobadores,
        string parametrosResueltos, bool pendienteAprobacion, CancellationToken ct);
    Task CerrarEjecucion(long ejecucionId, int filas, int duracionMs, string estado, CancellationToken ct);
    Task RegistrarEvento(long ejecucionId, string evento, string detalle, string resultado, int? filas, int? duracionMs, CancellationToken ct);
}

/// <summary>Ejecuta SOLO procedimientos almacenados contra una conexión cuya cadena vive en Key Vault.</summary>
public interface IConsultaExecutor
{
    Task<ResultadoEjecucion> Ejecutar(int conexionId, string procedimiento, IReadOnlyDictionary<string, object?> parametros,
        int tiempoMaximoSeg, int limiteFilas, CancellationToken ct);
}

public interface IExportador
{
    string Formato { get; }
    string TipoContenido { get; }
    byte[] Exportar(ResultadoEjecucion resultado);
}
