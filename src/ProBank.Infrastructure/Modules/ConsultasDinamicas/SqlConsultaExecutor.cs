using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Modules.ConsultasDinamicas;
using ProBank.Infrastructure.Persistence;
using ProBank.Infrastructure.Security;

namespace ProBank.Infrastructure.Modules.ConsultasDinamicas;

/// <summary>
/// SQL-01 y SQL-02: ejecuta únicamente procedimientos almacenados, con parámetros tipados, sobre una conexión cuya cadena se
/// lee de Key Vault en el momento (nunca viaja al navegador). También lee la definición del procedimiento y prueba la conexión.
/// </summary>
internal sealed class SqlConsultaExecutor(ISqlConnectionFactory db, ISecretProvider secretos) : IConsultaExecutor
{
    private async Task<SqlConnection> Abrir(string conexion, CancellationToken ct)
    {
        string secreto;
        await using (var cn = await db.Abrir(ct))
            secreto = await cn.ExecuteScalarAsync<string>(new CommandDefinition("rpt.usp_Conexion_ObtenerSecreto",
                new { Codigo = conexion }, commandType: Sp.Tipo, cancellationToken: ct))
                ?? throw new InvalidOperationException("La base de datos no existe o está inactiva.");
        var destino = new SqlConnection(await secretos.Obtener(secreto, ct));
        try { await destino.OpenAsync(ct); }
        catch { await destino.DisposeAsync(); throw; }
        return destino;
    }

    public async Task<ResultadoEjecucion> Ejecutar(string conexion, string procedimiento, IReadOnlyDictionary<string, object?> parametros,
        int tiempoMaximoSeg, int limiteFilas, CancellationToken ct)
    {
        if (!Consulta.EsProcedimientoValido(procedimiento)) throw new InvalidOperationException("Procedimiento no válido.");
        var sw = Stopwatch.StartNew();
        await using var destino = await Abrir(conexion, ct);
        await using var cmd = new SqlCommand(procedimiento, destino) { CommandType = CommandType.StoredProcedure, CommandTimeout = tiempoMaximoSeg };
        foreach (var (nombre, valor) in parametros)
            cmd.Parameters.AddWithValue(nombre.StartsWith('@') ? nombre : "@" + nombre, valor switch
            {
                null => DBNull.Value,
                DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                _ => valor
            });

        await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        var columnas = Enumerable.Range(0, rd.FieldCount).Select(i => string.IsNullOrEmpty(rd.GetName(i)) ? $"Columna{i + 1}" : rd.GetName(i)).ToList();
        var tipos = Enumerable.Range(0, rd.FieldCount).Select(i => Tipo(rd.GetDataTypeName(i))).ToList();
        var filas = new List<IReadOnlyDictionary<string, object?>>();
        var truncado = false;
        while (await rd.ReadAsync(ct))
        {
            if (filas.Count >= limiteFilas) { truncado = true; break; }
            var fila = new Dictionary<string, object?>(columnas.Count);
            for (var i = 0; i < columnas.Count; i++)
                fila[columnas[i]] = await rd.IsDBNullAsync(i, ct) ? null : Normalizar(rd.GetValue(i));
            filas.Add(fila);
        }
        if (truncado) cmd.Cancel();
        return new ResultadoEjecucion(columnas, tipos, filas, (int)sw.ElapsedMilliseconds, truncado);
    }

    /// <summary>Parámetros de entrada (sys.parameters) y columnas del primer resultado (sp_describe_first_result_set), sin ejecutarlo.</summary>
    public async Task<DefinicionProcedimiento?> LeerDefinicion(string conexion, string procedimiento, CancellationToken ct)
    {
        if (!Consulta.EsProcedimientoValido(procedimiento)) throw new InvalidOperationException("Procedimiento no válido.");
        await using var destino = await Abrir(conexion, ct);
        var id = await destino.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT OBJECT_ID(@p, 'P')", new { p = procedimiento }, cancellationToken: ct));
        if (id is null) return null;
        var parametros = (await destino.QueryAsync<(string Nombre, string Tipo)>(new CommandDefinition(
            "SELECT p.name, TYPE_NAME(p.user_type_id) FROM sys.parameters p WHERE p.object_id = @id AND p.is_output = 0 ORDER BY p.parameter_id",
            new { id }, cancellationToken: ct))).ToList();
        IReadOnlyList<ColumnaDto> columnas = [];
        try
        {
            // Nombre ya validado (esquema.nombre); se cita cada parte para el texto que se describe
            var partes = procedimiento.Split('.');
            columnas = (await destino.QueryAsync<(string? Nombre, string Tipo)>(new CommandDefinition(
                "SELECT name, system_type_name FROM sys.dm_exec_describe_first_result_set(@t, NULL, 0) WHERE is_hidden = 0 ORDER BY column_ordinal",
                new { t = $"EXEC {Cita(partes[0])}.{Cita(partes[1])}" }, cancellationToken: ct)))
                .Where(c => c.Nombre is not null).Select(c => new ColumnaDto(c.Nombre!, c.Tipo)).ToList();
        }
        catch (SqlException) { /* p. ej. tablas temporales: las columnas se conocen al ejecutar */ }
        return new DefinicionProcedimiento(parametros, columnas);
    }

    public async Task<PruebaConexionDto> Probar(string conexion, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var destino = await Abrir(conexion, ct);
            var (baseDatos, usuario) = await destino.QuerySingleAsync<(string, string)>(new CommandDefinition("SELECT DB_NAME(), SUSER_SNAME()", cancellationToken: ct));
            return new PruebaConexionDto(true, $"Conexión exitosa a {baseDatos} como {usuario} ({sw.ElapsedMilliseconds} ms)", (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new PruebaConexionDto(false, ex.Message.Length > 300 ? ex.Message[..300] : ex.Message, (int)sw.ElapsedMilliseconds);
        }
    }

    private static string Cita(string s) => "[" + s.Replace("]", "]]") + "]";

    private static string Tipo(string sql) => sql.ToLowerInvariant() switch
    {
        "date" => "fecha",
        "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "fechahora",
        "int" or "bigint" or "smallint" or "tinyint" => "entero",
        "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "numero",
        "bit" => "bool",
        _ => "texto",
    };

    // Valores que se guardan y se envían como JSON sin pérdida: lo demás (hora, guid, binarios…) va como texto
    private static object? Normalizar(object v) => v switch
    {
        DateTime or decimal or int or long or short or byte or bool or string => v,
        DateTimeOffset o => o.DateTime,
        double d => (decimal)d,
        float f => (decimal)f,
        byte[] => "(binario)",
        _ => v.ToString(),
    };
}
