using System.Text.Json;
using Dapper;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.ConsultasDinamicas;

internal sealed class ProgramacionRepository(ISqlConnectionFactory db) : IProgramacionRepository
{
    public async Task<IReadOnlyList<Programacion>> TomarVencidas(DateTime ahora, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        var filas = await cn.QueryAsync<Fila>(new CommandDefinition("rpt.usp_Programacion_TomarVencidas", new { Ahora = ahora }, commandType: Sp.Tipo, cancellationToken: ct));
        return filas.Select(f => new Programacion(f.ProgramacionId, f.Codigo, f.ConsultaCodigo, f.ConexionCodigo, Enum.Parse<Frecuencia>(f.Frecuencia),
            TimeOnly.FromTimeSpan(f.Hora),
            string.IsNullOrEmpty(f.Dias) ? [] : f.Dias.Split(',').Select(d => (DayOfWeek)int.Parse(d)).ToList(),
            f.DiaDelMes, f.Formato, f.Destinatarios.Split(',', StringSplitOptions.RemoveEmptyEntries),
            JsonSerializer.Deserialize<Dictionary<string, string?>>(f.ParametrosJson) ?? new Dictionary<string, string?>(), f.ResponsableId)).ToList();
    }

    public async Task MarcarEjecutada(int programacionId, DateTime proxima, string resultado, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("rpt.usp_Programacion_MarcarEjecutada",
            new { ProgramacionId = programacionId, Proxima = proxima, Resultado = resultado }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    private sealed record Fila(int ProgramacionId, string Codigo, string ConsultaCodigo, string ConexionCodigo, string Frecuencia, TimeSpan Hora, string? Dias,
        int? DiaDelMes, string Formato, string Destinatarios, string ParametrosJson, int ResponsableId);
}
