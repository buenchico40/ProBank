using ProBank.Application.Abstractions;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.Application.Modules.ConsultasDinamicas;

/// <summary>Resultado de un procedimiento. Tipos por columna: texto, numero, entero, fecha, fechahora o bool.</summary>
public sealed record ResultadoEjecucion(IReadOnlyList<string> Columnas, IReadOnlyList<string> Tipos,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Filas, int DuracionMs, bool Truncado);

/// <summary>Definición leída de la base: parámetros de entrada (sys.parameters) y columnas del primer resultado.</summary>
public sealed record DefinicionProcedimiento(IReadOnlyList<(string Nombre, string TipoSql)> Parametros, IReadOnlyList<ColumnaDto> Columnas);

/// <summary>Consulta lista para ejecutar, con los nombres que muestra el formulario.</summary>
public sealed record ConsultaEjecutable(Consulta Consulta, IReadOnlyList<OpcionDto> Conexiones, IReadOnlyList<string> NombresAprobadores);

/// <summary>Una ejecución con sus datos internos (la API decide quién la ve y qué puede hacer).</summary>
public sealed record EjecucionInterna(long EjecucionId, string Codigo, string ConsultaCodigo, string ConsultaNombre, string Procedimiento,
    string ConexionCodigo, string Base, int SolicitanteId, string SolicitadaPor, DateTime Fecha, string ParametrosResueltos, string ParametrosJson,
    bool Programada, int? Filas, int? DuracionMs, bool Truncado, string? RutaResultado, string? Mensaje, string Estado,
    IReadOnlyList<(int UsuarioId, AprobacionDto Aprobacion)> Aprobaciones, IReadOnlyList<EventoEjecucionDto> Eventos);

public sealed record OpcionesBase(IReadOnlyList<string> Roles, IReadOnlyList<UsuarioOpcionDto> Usuarios, IReadOnlyList<OpcionDto> Conexiones);

public interface IConsultaRepository
{
    // Bases de datos
    Task<IReadOnlyList<ConexionDto>> ListarConexiones(CancellationToken ct);
    Task<ConexionDetalleDto?> DetalleConexion(string codigo, CancellationToken ct);
    Task<string> GuardarConexion(string? codigo, GuardarConexionRequest r, Actor actor, CancellationToken ct);
    Task EliminarConexion(string codigo, Actor actor, CancellationToken ct);

    // Catálogo
    Task<IReadOnlyList<ConsultaDto>> ListarConsultas(CancellationToken ct);
    Task<ConsultaDetalleDto?> DetalleConsulta(string codigo, CancellationToken ct);
    Task<(string Codigo, bool VolvioBorrador)> GuardarConsulta(string? codigo, GuardarConsultaRequest r, Actor actor, CancellationToken ct);
    /// <summary>Devuelve si la consulta volvió a Borrador porque cambiaron sus parámetros.</summary>
    Task<bool> GuardarDefinicion(string codigo, string conexion, IReadOnlyList<(string Nombre, string TipoSql, string Tipo)> parametros,
        IReadOnlyList<ColumnaDto> columnas, Actor actor, CancellationToken ct);
    Task GuardarParametros(string codigo, IReadOnlyList<ParametroDto> parametros, Actor actor, CancellationToken ct);
    Task CambiarEstadoConsulta(string codigo, string accion, Actor actor, CancellationToken ct);
    Task EliminarConsulta(string codigo, Actor actor, CancellationToken ct);
    Task<IReadOnlyList<ConsultaDisponibleDto>> Disponibles(string rolActivo, CancellationToken ct);
    Task<OpcionesBase> Opciones(CancellationToken ct);
    Task<ConsultaEjecutable?> Obtener(string codigo, CancellationToken ct);

    // Ejecuciones y auditoría
    Task<(long Id, string Codigo)> CrearEjecucion(int consultaId, string conexion, int solicitanteId, IReadOnlyList<int> aprobadores,
        string parametrosResueltos, string parametrosJson, bool programada, bool pendienteAprobacion, CancellationToken ct);
    Task CerrarEjecucion(long ejecucionId, int? filas, int? duracionMs, bool truncado, string? rutaResultado, string? mensaje, string estado, CancellationToken ct);
    Task RegistrarEvento(long ejecucionId, string evento, string detalle, string resultado, int? filas, int? duracionMs, CancellationToken ct);
    Task<IReadOnlyList<EjecucionDto>> ListarEjecuciones(int usuarioId, bool todas, CancellationToken ct);
    Task<EjecucionInterna?> ObtenerEjecucion(string codigo, CancellationToken ct);
    /// <summary>Devuelve el estado que queda: PendienteAprobacion, Aprobada o Rechazada.</summary>
    Task<string> ResolverEjecucion(string codigo, int usuarioId, bool aprobar, string? comentario, string ip, CancellationToken ct);
    Task<IReadOnlyList<EventoEjecucionDto>> ListarEventos(DateOnly desde, DateOnly hasta, CancellationToken ct);
    /// <summary>CFG-04: resultados guardados con más de <paramref name="dias"/> días (de a 200).</summary>
    Task<IReadOnlyList<(long EjecucionId, string Codigo, string Ruta)>> ResultadosVencidos(int dias, CancellationToken ct);
    Task QuitarResultado(long ejecucionId, int dias, CancellationToken ct);

    // Envíos programados
    Task<string> GuardarProgramacion(string? codigo, string consulta, GuardarProgramacionRequest r, DateTime proxima, Actor actor, CancellationToken ct);
    Task EliminarProgramacion(string codigo, Actor actor, CancellationToken ct);
}

/// <summary>Ejecuta SOLO procedimientos almacenados contra una conexión cuya cadena vive en Key Vault.</summary>
public interface IConsultaExecutor
{
    Task<ResultadoEjecucion> Ejecutar(string conexion, string procedimiento, IReadOnlyDictionary<string, object?> parametros,
        int tiempoMaximoSeg, int limiteFilas, CancellationToken ct);
    /// <summary>Lee los parámetros y las columnas del primer resultado del procedimiento; null si no existe en esa base.</summary>
    Task<DefinicionProcedimiento?> LeerDefinicion(string conexion, string procedimiento, CancellationToken ct);
    Task<PruebaConexionDto> Probar(string conexion, CancellationToken ct);
}

public interface IExportador
{
    string Formato { get; }
    string TipoContenido { get; }
    byte[] Exportar(ResultadoEjecucion resultado, string titulo);
}
