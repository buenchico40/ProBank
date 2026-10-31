using ProBank.Application.Abstractions;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.Api.BackgroundJobs;

/// <summary>
/// SQL-07 y SQL-08: cada minuto toma las programaciones vencidas, resuelve sus variables, ejecuta el procedimiento,
/// exporta en el formato elegido y envía el correo con Graph. Cada corrida queda en la auditoría de ejecuciones.
/// </summary>
internal sealed class ProgramadorConsultas(IServiceScopeFactory scopes, ILogger<ProgramadorConsultas> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { await Ciclo(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Error en el programador de consultas"); }
        }
    }

    private async Task Ciclo(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var sp = scope.ServiceProvider;
        var clock = sp.GetRequiredService<IClock>();
        var programaciones = sp.GetRequiredService<IProgramacionRepository>();
        var consultas = sp.GetRequiredService<IConsultaRepository>();
        var executor = sp.GetRequiredService<IConsultaExecutor>();
        var mail = sp.GetRequiredService<IMailSender>();
        var exportadores = sp.GetServices<IExportador>().ToDictionary(e => e.Formato);

        foreach (var p in await programaciones.TomarVencidas(clock.Ahora, ct))
        {
            var proxima = CalendarioProgramacion.Proxima(p, clock.Ahora);
            var consulta = await consultas.Obtener(p.ConsultaCodigo, ct);
            if (consulta is null) { await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, "Consulta inexistente", ct); continue; }

            var enviados = p.Parametros.Select(kv => new ProBank.Contracts.ConsultasDinamicas.ParametroValor(kv.Key,
                VariablesParametro.EsVariable(kv.Value) ? null : kv.Value, VariablesParametro.EsVariable(kv.Value) ? kv.Value : null)).ToList();
            var valores = ResolverParametros.Resolver(consulta.Parametros, enviados, clock.Hoy);
            if (valores.IsFailure) { await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, valores.Error.Message, ct); continue; }

            var (id, codigo) = await consultas.CrearEjecucion(consulta.ConsultaId, p.ConexionId, p.ResponsableId, [], valores.Value.Texto, false, ct);
            await consultas.RegistrarEvento(id, "EjecucionProgramada", valores.Value.Texto, "Registrada", null, null, ct);
            try
            {
                var r = await executor.Ejecutar(p.ConexionId, consulta.Procedimiento, valores.Value.Valores, consulta.TiempoMaximoSeg, consulta.LimiteFilas, ct);
                await consultas.CerrarEjecucion(id, r.Filas.Count, r.DuracionMs, "Ejecutada", ct);
                var exp = exportadores.GetValueOrDefault(p.Formato) ?? exportadores["csv"];
                var archivo = exp.Exportar(r);
                await mail.Enviar(p.Destinatarios, $"ProBank · {consulta.Nombre}",
                    $"<p>Resultado de <b>{consulta.Nombre}</b> ({r.Filas.Count} filas).</p><p>Parámetros: {valores.Value.Texto}</p>",
                    ($"{consulta.Codigo}_{codigo}.{exp.Formato}", archivo, exp.TipoContenido), ct);
                await consultas.RegistrarEvento(id, "EnvioCorreo", string.Join(", ", p.Destinatarios), "Exitosa", r.Filas.Count, r.DuracionMs, ct);
                await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, "Exitosa", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await consultas.RegistrarEvento(id, "EjecucionProgramada", ex.Message, "Fallida", null, null, CancellationToken.None);
                await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, "Fallida", CancellationToken.None);
            }
        }
    }
}
