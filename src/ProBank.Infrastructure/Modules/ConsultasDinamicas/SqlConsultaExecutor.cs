using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Domain.Modules.ConsultasDinamicas;
using ProBank.Infrastructure.Persistence;
using ProBank.Infrastructure.Security;

namespace ProBank.Infrastructure.Modules.ConsultasDinamicas;

/// <summary>
/// SQL-01 y SQL-02: ejecuta únicamente procedimientos almacenados, con parámetros tipados,
/// sobre una conexión cuya cadena se lee de Key Vault en el momento (nunca viaja al navegador).
/// </summary>
internal sealed class SqlConsultaExecutor(ISqlConnectionFactory db, ISecretProvider secretos) : IConsultaExecutor
{
    public async Task<ResultadoEjecucion> Ejecutar(int conexionId, string procedimiento, IReadOnlyDictionary<string, object?> parametros,
        int tiempoMaximoSeg, int limiteFilas, CancellationToken ct)
    {
        if (!Consulta.EsProcedimientoValido(procedimiento))
            throw new InvalidOperationException("Procedimiento no válido.");

        string secreto;
        await using (var cn = await db.Abrir(ct))
            secreto = await cn.ExecuteScalarAsync<string>(new CommandDefinition("rpt.usp_Conexion_ObtenerSecreto",
                new { ConexionId = conexionId }, commandType: Sp.Tipo, cancellationToken: ct))
                ?? throw new InvalidOperationException("La conexión no existe o está inactiva.");

        var cadena = await secretos.Obtener(secreto, ct);
        var sw = Stopwatch.StartNew();
        await using var destino = new SqlConnection(cadena);
        await destino.OpenAsync(ct);
        await using var cmd = new SqlCommand(procedimiento, destino) { CommandType = CommandType.StoredProcedure, CommandTimeout = tiempoMaximoSeg };
        foreach (var (nombre, valor) in parametros)
            cmd.Parameters.AddWithValue(nombre.StartsWith('@') ? nombre : "@" + nombre, valor switch
            {
                null => DBNull.Value,
                DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                _ => valor
            });

        await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        var columnas = Enumerable.Range(0, rd.FieldCount).Select(rd.GetName).ToList();
        var filas = new List<IReadOnlyDictionary<string, object?>>();
        var truncado = false;
        while (await rd.ReadAsync(ct))
        {
            if (filas.Count >= limiteFilas) { truncado = true; break; }
            var fila = new Dictionary<string, object?>(columnas.Count);
            for (var i = 0; i < columnas.Count; i++)
                fila[columnas[i]] = await rd.IsDBNullAsync(i, ct) ? null : rd.GetValue(i);
            filas.Add(fila);
        }
        return new ResultadoEjecucion(columnas, filas, (int)sw.ElapsedMilliseconds, truncado);
    }
}
