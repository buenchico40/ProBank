using System.Net;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Configuracion;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Modules.Configuracion;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.Api.BackgroundJobs;

/// <summary>
/// SQL-07 y SQL-08: cada minuto toma las programaciones vencidas, resuelve sus variables, ejecuta el procedimiento,
/// exporta en el formato elegido y envía el correo con Graph. Cada corrida queda en la auditoría de ejecuciones.
/// </summary>
internal sealed class ProgramadorConsultas(IServiceScopeFactory scopes, ILogger<ProgramadorConsultas> log) : BackgroundService
{
    private DateTime _ultimaLimpieza = DateTime.MinValue;

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
        var almacen = sp.GetRequiredService<IAlmacenArchivos>();
        var exportadores = sp.GetServices<IExportador>().ToDictionary(e => e.Formato);
        var cfg = await sp.GetRequiredService<IConfiguracionGlobal>().Valores(ct);

        // CFG-04: una vez por hora, borra los resultados guardados que vencieron (la auditoría se conserva)
        if (clock.Ahora - _ultimaLimpieza >= TimeSpan.FromHours(1))
        {
            _ultimaLimpieza = clock.Ahora;
            var dias = ReglasConfiguracion.Entero(cfg, ReglasConfiguracion.DiasConservacion, 90);
            if (dias > 0)
                foreach (var (ejecucionId, codigo, ruta) in await consultas.ResultadosVencidos(dias, ct))
                {
                    await MotorConsultas.Eliminar(almacen, ruta, ct);
                    await consultas.QuitarResultado(ejecucionId, dias, ct);
                    log.LogInformation("Resultado de {Ejecucion} eliminado por conservación de {Dias} días", codigo, dias);
                }
        }

        foreach (var p in await programaciones.TomarVencidas(clock.Ahora, ct))
        {
            var proxima = CalendarioProgramacion.Proxima(p, clock.Ahora);
            if (await consultas.Obtener(p.ConsultaCodigo, ct) is not { } x) { await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, "Consulta inexistente", ct); continue; }
            var consulta = x.Consulta;
            // Las consultas con aprobadores no se envían programadas (al agregarlos, sus envíos quedan en pausa)
            if (consulta.Aprobadores.Count > 0) { await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, "Omitida: la consulta requiere aprobación", ct); continue; }

            // SQL-07: las variables se resuelven en cada corrida
            var enviados = p.Parametros.Select(kv => new ParametroValor(kv.Key,
                VariablesParametro.EsVariable(kv.Value) ? null : kv.Value, VariablesParametro.EsVariable(kv.Value) ? kv.Value : null)).ToList();
            var valores = ResolverParametros.Resolver(consulta.Parametros, enviados, clock.Hoy);
            if (valores.IsFailure) { await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, valores.Error.Message, ct); continue; }

            var (id, codigo) = await consultas.CrearEjecucion(consulta.ConsultaId, p.ConexionCodigo, p.ResponsableId, [], valores.Value.Texto, valores.Value.Json, true, false, ct);
            await consultas.RegistrarEvento(id, "Ejecución programada", $"{p.Codigo} · {valores.Value.Texto}", "Registrada", null, null, ct);
            var r = await MotorConsultas.Correr(consultas, executor, almacen, consulta, id, codigo, p.ConexionCodigo, valores.Value.Valores, valores.Value.Texto, ct);
            if (r.IsFailure) { await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, "Fallida", CancellationToken.None); continue; }
            try
            {
                // SQL-08: envío por correo (proveedor de Configuración › Parámetros generales) con el archivo en el formato elegido;
                // CFG-04: si supera el máximo configurado, sale sin adjunto y el responsable lo descarga desde ProBank
                var exp = exportadores.GetValueOrDefault(p.Formato) ?? exportadores["csv"];
                var archivo = exp.Exportar(r.Value, $"{consulta.Codigo} · {consulta.Nombre} · {codigo}");
                var maximoMB = ReglasConfiguracion.Entero(cfg, ReglasConfiguracion.MaximoMBAdjunto, 10);
                var adjunta = archivo.Length <= maximoMB * 1024 * 1024;
                await mail.Enviar(p.Destinatarios, $"ProBank · {consulta.Nombre}",
                    $"<p>Resultado de <b>{WebUtility.HtmlEncode(consulta.Nombre)}</b> ({r.Value.Filas.Count} filas{(r.Value.Truncado ? ", truncado al límite" : "")}).</p>" +
                    $"<p>Parámetros: {WebUtility.HtmlEncode(valores.Value.Texto)}</p><p>Envío programado {p.Codigo} · ejecución {codigo}.</p>" +
                    (adjunta ? "" : $"<p>El archivo ({archivo.Length / 1024 / 1024} MB) supera el máximo de {maximoMB} MB para adjuntar: el responsable del envío lo descarga " +
                                    $"desde ProBank, en Consultas dinámicas › Ejecuciones y aprobaciones, ejecución {codigo}.</p>"),
                    adjunta ? ($"{consulta.Codigo}_{codigo}.{exp.Formato}", archivo, exp.TipoContenido) : null, ct);
                await consultas.RegistrarEvento(id, "Envío por correo", string.Join(", ", p.Destinatarios) + (adjunta ? "" : $" · sin adjunto (supera {maximoMB} MB)"),
                    "Exitosa", r.Value.Filas.Count, r.Value.DuracionMs, ct);
                await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, "Exitosa", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await consultas.RegistrarEvento(id, "Envío por correo", ex.Message.Length > 450 ? ex.Message[..450] : ex.Message, "Fallida", null, null, CancellationToken.None);
                await programaciones.MarcarEjecutada(p.ProgramacionId, proxima, "Envío fallido", CancellationToken.None);
            }
        }
    }
}
