namespace ProBank.Contracts.ConsultasDinamicas;

public sealed record ParametroValor(string Nombre, string? Valor, string? Variable);

public sealed record EjecutarConsultaRequest(string ConsultaCodigo, int ConexionId, IReadOnlyList<ParametroValor> Parametros);

public sealed record EjecucionDto(string Codigo, string Estado, string ParametrosResueltos, int? Filas, string? Aprobaciones);

public sealed record ResultadoConsultaDto(string Ejecucion, IReadOnlyList<string> Columnas,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Filas, int DuracionMs, bool Truncado);
