using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Portafolio;
using ProBank.Contracts.Portafolio;
using ProBank.Domain.Modules.Portafolio;

namespace ProBank.Api.Controllers.Portafolio;

[ApiController]
[Route("api/portafolio/tareas")]
public sealed class TareasController(IDispatcher dispatcher) : ControllerBase
{
    /// <summary>Tareas de un ítem; sin ítem, «Mis tareas» (o todas con <paramref name="todas"/> y permiso de edición).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] Guid? item, [FromQuery] bool todas, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new ListarTareasQuery(item, todas), ct));

    [HttpGet("{token:guid}")]
    public async Task<IActionResult> Detalle(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetalleTareaQuery(token), ct));

    /// <summary>PRY-03: crear una tarea dentro de un proyecto, mejora o bug.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear(CrearTareaRequest request, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new CrearTareaCommand(request), ct));

    [HttpPut("{token:guid}")]
    public async Task<IActionResult> Actualizar(Guid token, ActualizarTareaRequest request, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new ActualizarTareaCommand(token, request), ct));

    [HttpPost("{token:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EnviarTareaCommand(token), ct));

    [HttpPost("{token:guid}/completar")]
    public async Task<IActionResult> Completar(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new CompletarTareaCommand(token), ct));

    /// <summary>PRY-04: registra la aprobación del usuario actual; la tarea pasa a En curso cuando aprueban todos.</summary>
    [HttpPost("{token:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new AprobarTareaCommand(token), ct));

    /// <summary>PRY-05: rechazo con comentario obligatorio.</summary>
    [HttpPost("{token:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid token, RechazarRequest request, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new RechazarTareaCommand(token, request.Comentario), ct));

    [HttpGet("~/api/portafolio/aprobaciones")]
    public async Task<IActionResult> Aprobaciones(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarAprobacionesQuery(), ct));
}

/// <summary>Proyectos, mejoras y bugs, cronograma, acuerdos semanales, informes y parametrización.</summary>
[ApiController]
[Route("api/portafolio")]
public sealed class PortafolioController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("items")]
    public async Task<IActionResult> Items([FromQuery] bool abiertos = false, CancellationToken ct = default) => this.ToActionResult(await dispatcher.Query(new ListarItemsQuery(abiertos), ct));

    [HttpGet("items/{token:guid}")]
    public async Task<IActionResult> Item(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerItemQuery(token), ct));

    [HttpPost("items")]
    public async Task<IActionResult> CrearItem(GuardarItemRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarItemCommand(null, r), ct));

    [HttpPut("items/{token:guid}")]
    public async Task<IActionResult> EditarItem(Guid token, GuardarItemRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarItemCommand(token, r), ct));

    [HttpDelete("items/{token:guid}")]
    public async Task<IActionResult> EliminarItem(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarItemCommand(token), ct));

    /// <summary>Documentos de un ítem («items») o de una tarea («tareas»).</summary>
    [HttpGet("{dueno:regex(^(items|tareas)$)}/{token:guid}/adjuntos")]
    public async Task<IActionResult> Adjuntos(string dueno, Guid token, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new ListarAdjuntosQuery(Ambito(dueno), token), ct));

    /// <summary>Tipos y tamaño máximo según Portafolio › Parametrización (la validación la hace el comando).</summary>
    [HttpPost("{dueno:regex(^(items|tareas)$)}/{token:guid}/adjuntos")]
    [RequestSizeLimit(21_000_000)]
    public async Task<IActionResult> SubirAdjunto(string dueno, Guid token, IFormFile archivo, [FromForm] string? descripcion, CancellationToken ct)
    {
        if (archivo.Length > ReglasAdjunto.MaximoMBPermitido * 1024L * 1024L)
            return Problem(statusCode: 422, title: "tamano", detail: $"El archivo supera el máximo de {ReglasAdjunto.MaximoMBPermitido} MB.");
        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        return this.ToActionResult(await dispatcher.Send(new SubirAdjuntoCommand(Ambito(dueno), token, archivo.FileName, descripcion, ms.ToArray()), ct));
    }

    [HttpPut("adjuntos/{token:guid}")]
    public async Task<IActionResult> ActualizarAdjunto(Guid token, ActualizarAdjuntoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new ActualizarAdjuntoCommand(token, r.Descripcion), ct));

    private static AmbitoAdjunto Ambito(string dueno) => dueno == "items" ? AmbitoAdjunto.Item : AmbitoAdjunto.Tarea;

    [HttpGet("adjuntos/parametros")]
    public async Task<IActionResult> ParametrosAdjunto(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ParametrosAdjuntoQuery(), ct));

    [HttpPut("adjuntos/parametros/{ambito}")]
    public async Task<IActionResult> GuardarParametroAdjunto(string ambito, GuardarParametroAdjuntoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarParametroAdjuntoCommand(ambito, r), ct));

    [HttpGet("adjuntos/{token:guid}")]
    public async Task<IActionResult> DescargarAdjunto(Guid token, CancellationToken ct)
    {
        var r = await dispatcher.Query(new DescargarAdjuntoQuery(token), ct);
        return r.IsSuccess ? File(r.Value.Contenido, r.Value.TipoContenido, r.Value.Nombre) : this.ToActionResult(r);
    }

    [HttpDelete("adjuntos/{token:guid}")]
    public async Task<IActionResult> EliminarAdjunto(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarAdjuntoCommand(token), ct));

    [HttpPut("items/{token:guid}/hitos")]
    public async Task<IActionResult> Hitos(Guid token, GuardarHitosRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarHitosCommand(token, r.Hitos), ct));

    /// <summary>PRY-07: cronograma de todos los ítems o de uno.</summary>
    [HttpGet("cronograma")]
    public async Task<IActionResult> Cronograma([FromQuery] Guid? item, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new CronogramaQuery(item), ct));

    [HttpGet("acuerdos")]
    public async Task<IActionResult> Acuerdos(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarAcuerdosQuery(), ct));

    [HttpPost("acuerdos")]
    public async Task<IActionResult> RegistrarAcuerdos(RegistrarAcuerdosRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new RegistrarAcuerdosCommand(r), ct));

    [HttpPut("acuerdos/{id:int}")]
    public async Task<IActionResult> ActualizarAcuerdo(int id, ActualizarAcuerdoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new ActualizarAcuerdoCommand(id, r), ct));

    [HttpDelete("acuerdos/{id:int}")]
    public async Task<IActionResult> EliminarAcuerdo(int id, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarAcuerdoCommand(id), ct));

    [HttpPost("acuerdos/{id:int}/cumplir")]
    public async Task<IActionResult> CumplirAcuerdo(int id, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new CumplirAcuerdoCommand(id), ct));

    [HttpGet("informes")]
    public async Task<IActionResult> Informes(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarInformesQuery(), ct));

    [HttpGet("informes/{id:int}")]
    public async Task<IActionResult> Informe(int id, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerInformeQuery(id), ct));

    [HttpGet("informes/{id:int}/resumen")]
    public async Task<IActionResult> ResumenInforme(int id, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ResumenInformeQuery(id), ct));

    [HttpPost("informes")]
    public async Task<IActionResult> CrearInforme(GuardarInformeRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarInformeCommand(null, r), ct));

    [HttpPut("informes/{id:int}")]
    public async Task<IActionResult> EditarInforme(int id, GuardarInformeRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarInformeCommand(id, r), ct));

    [HttpPost("informes/{id:int}/generar")]
    public async Task<IActionResult> GenerarInforme(int id, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GenerarInformeCommand(id), ct));

    /// <summary>PDF del informe generado (DevExpress Reporting).</summary>
    [HttpGet("informes/{id:int}/pdf")]
    public async Task<IActionResult> InformePdf(int id, CancellationToken ct)
    {
        var r = await dispatcher.Query(new InformePdfQuery(id), ct);
        return r.IsSuccess ? File(r.Value.Contenido, r.Value.TipoContenido, r.Value.Nombre) : this.ToActionResult(r);
    }

    /// <summary>PRY-09: qué roles ejecutan cada acción.</summary>
    [HttpGet("parametrizacion")]
    public async Task<IActionResult> Parametrizacion(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ParametrizacionQuery(), ct));

    [HttpPut("parametrizacion/{accion}")]
    public async Task<IActionResult> GuardarAccion(string accion, GuardarAccionRolRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarAccionRolCommand(accion, r.Roles), ct));
}
