namespace ProBank.Contracts.ConsultasDinamicas;

// ---------- Bases de datos (SQL-02): nunca viaja la cadena, solo el nombre del secreto de Key Vault ----------
public sealed record ConexionDto(string Codigo, string Nombre, string Servidor, string BaseDatos, string Autenticacion, string SecretoKeyVault,
    string Estado, int Consultas);
public sealed record ConsultaResumenDto(string Codigo, string Nombre, string Estado);
public sealed record ConexionDetalleDto(ConexionDto Conexion, IReadOnlyList<ConsultaResumenDto> Consultas);
public sealed record GuardarConexionRequest(string Nombre, string Servidor, string BaseDatos, string Autenticacion, string SecretoKeyVault, string Estado);
public sealed record PruebaConexionDto(bool Exitosa, string Mensaje, int Milisegundos);

// ---------- Catálogo de consultas (SQL-01, SQL-03, SQL-04) ----------
public sealed record ConsultaDto(string Codigo, string Nombre, string? Descripcion, string Procedimiento, string Conexiones, string Roles,
    string Aprobadores, int TiempoMaximoSeg, int LimiteFilas, string Estado, int Parametros, bool DefinicionLeida);
/// <summary>Tipo: Texto, Numero, Fecha o Lista; ValorDefecto fijo o variable (Hoy, Fin de mes…); Opciones separadas por coma.</summary>
public sealed record ParametroDto(string Nombre, string Etiqueta, string Tipo, string? TipoSql, bool Obligatorio, string? ValorDefecto, string? Opciones);
public sealed record ColumnaDto(string Nombre, string Tipo);
public sealed record OpcionDto(string Codigo, string Nombre, string Estado);
public sealed record UsuarioOpcionDto(Guid Token, string Nombre, string? Correo);
public sealed record EjecucionResumenDto(string Codigo, string Base, string SolicitadaPor, DateTime Fecha, string Estado);
public sealed record ConsultaDetalleDto(string Codigo, string Nombre, string? Descripcion, string Procedimiento, int TiempoMaximoSeg, int LimiteFilas,
    string Estado, DateTime? DefinicionLeida, string CreadoPor, DateTime FechaRegistro,
    IReadOnlyList<OpcionDto> Conexiones, IReadOnlyList<string> Roles, IReadOnlyList<UsuarioOpcionDto> Aprobadores,
    IReadOnlyList<ParametroDto> Parametros, IReadOnlyList<ColumnaDto> Columnas, IReadOnlyList<EjecucionResumenDto> Ultimas,
    IReadOnlyList<ProgramacionDto> Programaciones);
public sealed record GuardarConsultaRequest(string Nombre, string? Descripcion, string Procedimiento, int TiempoMaximoSeg, int LimiteFilas,
    IReadOnlyList<string> Conexiones, IReadOnlyList<string> Roles, IReadOnlyList<Guid> Aprobadores);
public sealed record GuardarParametrosRequest(IReadOnlyList<ParametroDto> Parametros);
/// <summary>Variable de parámetro (SQL-04) con su valor de hoy: Tipo Fecha o Numero.</summary>
public sealed record VariableDto(string Nombre, string Tipo, string Valor);
public sealed record OpcionesConsultaDto(IReadOnlyList<string> Roles, IReadOnlyList<UsuarioOpcionDto> Usuarios, IReadOnlyList<OpcionDto> Conexiones,
    IReadOnlyList<VariableDto> Variables, IReadOnlyList<string> Formatos, IReadOnlyList<string> Autenticaciones);
public sealed record ConsultaDisponibleDto(string Codigo, string Nombre, string? Descripcion, string Procedimiento, int Aprobadores);
/// <summary>Lo que necesita el formulario de ejecución: bases activas permitidas, aprobadores (nombres) y parámetros.</summary>
public sealed record ConsultaParaEjecutarDto(string Codigo, string Nombre, string Procedimiento, int TiempoMaximoSeg, int LimiteFilas,
    IReadOnlyList<OpcionDto> Conexiones, IReadOnlyList<string> Aprobadores, IReadOnlyList<ParametroDto> Parametros, IReadOnlyList<VariableDto> Variables);

// ---------- Ejecuciones, aprobaciones y auditoría (SQL-03, SQL-05, SQL-06) ----------
/// <summary>Valor de un parámetro: fijo (Valor; fechas en ISO aaaa-mm-dd) o variable (Variable: Hoy, Fin de mes…).</summary>
public sealed record ParametroValor(string Nombre, string? Valor, string? Variable);
public sealed record EjecutarConsultaRequest(string ConsultaCodigo, string Conexion, IReadOnlyList<ParametroValor> Parametros);
/// <summary>Tipos: texto, numero, entero, fecha, fechahora o bool (para dar formato a la grilla y a la exportación).</summary>
public sealed record ResultadoConsultaDto(string Ejecucion, string Estado, IReadOnlyList<string> Columnas, IReadOnlyList<string> Tipos,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Filas, int DuracionMs, bool Truncado, string ParametrosResueltos);
public sealed record EjecucionDto(string Codigo, string ConsultaCodigo, string ConsultaNombre, string Base, string Parametros, string SolicitadaPor,
    DateTime Fecha, int Aprobadas, int Aprobadores, int? Filas, string Estado, bool Programada, bool EsMia, bool MeToca, bool TieneResultado);
public sealed record AprobacionDto(string Aprobador, string? Decision, DateTime? Fecha, string? Comentario);
public sealed record EventoEjecucionDto(DateTime Fecha, string Usuario, string Evento, string? Ejecucion, string? Consulta, string? Base, string? Detalle,
    int? Filas, int? DuracionMs, string Resultado, string? Ip);
public sealed record EjecucionDetalleDto(string Codigo, string ConsultaCodigo, string ConsultaNombre, string Procedimiento, string Base, string SolicitadaPor,
    DateTime Fecha, string Parametros, bool Programada, int? Filas, int? DuracionMs, bool Truncado, string? Mensaje, string Estado,
    bool EsMia, bool MeToca, bool TieneResultado, IReadOnlyList<AprobacionDto> Aprobaciones, IReadOnlyList<EventoEjecucionDto> Eventos);
public sealed record ResolverEjecucionRequest(bool Aprobar, string? Comentario);

// ---------- Envíos programados (SQL-07, SQL-08) ----------
/// <summary>Hora HH:mm (Ecuador); Dias 0 = domingo … 6 = sábado; DiaDelMes 0 = último día; Parametros: valor fijo o variable por parámetro.</summary>
public sealed record ProgramacionDto(string Codigo, string ConexionCodigo, string Base, string Frecuencia, string Hora, IReadOnlyList<int> Dias, int? DiaDelMes,
    string Formato, IReadOnlyDictionary<string, string?> Parametros, IReadOnlyList<string> Destinatarios, string Responsable,
    DateTime ProximaEjecucion, DateTime? UltimaEjecucion, string? UltimoResultado, string Estado);
public sealed record GuardarProgramacionRequest(string Conexion, string Frecuencia, string Hora, IReadOnlyList<int> Dias, int? DiaDelMes, string Formato,
    IReadOnlyDictionary<string, string?> Parametros, IReadOnlyList<string> Destinatarios, bool Activa);
