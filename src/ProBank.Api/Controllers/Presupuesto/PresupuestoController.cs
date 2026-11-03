using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Presupuesto;
using ProBank.Contracts.Presupuesto;
using ProBank.Domain.Modules.Presupuesto;

namespace ProBank.Api.Controllers.Presupuesto;

/// <summary>Presupuesto (COM-01 a COM-04): partidas, ajustes y movimientos, renovaciones, solicitudes de compra y pagos.</summary>
[ApiController]
[Route("api/presupuesto")]
public sealed class PresupuestoController(IDispatcher dispatcher) : ControllerBase
{
    // ---------- Áreas del presupuesto ----------
    [HttpGet("areas")]
    public async Task<IActionResult> Areas([FromQuery] bool soloActivas, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarAreasQuery(soloActivas), ct));

    [HttpPost("areas")]
    public async Task<IActionResult> CrearArea(GuardarAreaRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarAreaCommand(true, r), ct));

    /// <summary>Cambia el nombre o si está activa; el código no cambia.</summary>
    [HttpPut("areas/{codigo}")]
    public async Task<IActionResult> EditarArea(string codigo, GuardarAreaRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarAreaCommand(false, r with { Codigo = codigo }), ct));

    // ---------- Partidas ----------
    [HttpGet("partidas")]
    public async Task<IActionResult> Partidas([FromQuery] int? anio, [FromQuery] string? area, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarPartidasQuery(anio, area), ct));

    [HttpGet("partidas/{token:guid}")]
    public async Task<IActionResult> Partida(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetallePartidaQuery(token), ct));

    [HttpPost("partidas")]
    public async Task<IActionResult> CrearPartida(GuardarPartidaRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarPartidaCommand(null, r), ct));

    [HttpPut("partidas/{token:guid}")]
    public async Task<IActionResult> EditarPartida(Guid token, GuardarPartidaRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarPartidaCommand(token, r), ct));

    /// <summary>Aprobar (Borrador → Activo) o Cerrar (Activo → Cerrado).</summary>
    [HttpPost("partidas/{token:guid}/{accion:regex(^(Aprobar|Cerrar)$)}")]
    public async Task<IActionResult> EstadoPartida(Guid token, string accion, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new CambiarEstadoPartidaCommand(token, accion), ct));

    [HttpDelete("partidas/{token:guid}")]
    public async Task<IActionResult> EliminarPartida(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarPartidaCommand(token), ct));

    /// <summary>COM-01: borrador del año siguiente de un área, copiado de su presupuesto vigente.</summary>
    [HttpPost("partidas/preparar")]
    public async Task<IActionResult> Preparar([FromQuery] string area, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new PrepararBorradorCommand(area), ct));

    /// <summary>Reporte PDF (DevExpress Reporting) de un área y un año: partidas, servicios y pagos frente a lo presupuestado.</summary>
    [HttpGet("reporte")]
    public async Task<IActionResult> Reporte([FromQuery] string area, [FromQuery] int anio, CancellationToken ct)
    {
        var r = await dispatcher.Query(new ReportePresupuestoQuery(area, anio), ct);
        return r.IsSuccess ? File(r.Value.Contenido, r.Value.TipoContenido, r.Value.Nombre) : this.ToActionResult(r);
    }

    // ---------- Ajustes y movimientos ----------
    [HttpGet("movimientos")]
    public async Task<IActionResult> Movimientos(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarMovimientosQuery(), ct));

    [HttpPost("movimientos")]
    public async Task<IActionResult> CrearMovimiento(GuardarMovimientoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarMovimientoCommand(null, r), ct));

    [HttpPut("movimientos/{token:guid}")]
    public async Task<IActionResult> EditarMovimiento(Guid token, GuardarMovimientoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarMovimientoCommand(token, r), ct));

    [HttpPost("movimientos/{token:guid}/resolver")]
    public async Task<IActionResult> ResolverMovimiento(Guid token, ResolverRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new ResolverMovimientoCommand(token, r), ct));

    [HttpPost("movimientos/{token:guid}/anular")]
    public async Task<IActionResult> AnularMovimiento(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new AnularMovimientoCommand(token), ct));

    // ---------- Renovaciones ----------
    [HttpGet("renovaciones")]
    public async Task<IActionResult> Renovaciones(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarRenovacionesQuery(), ct));

    [HttpPost("renovaciones")]
    public async Task<IActionResult> CrearRenovacion(GuardarRenovacionRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarRenovacionCommand(null, r), ct));

    [HttpPut("renovaciones/{token:guid}")]
    public async Task<IActionResult> EditarRenovacion(Guid token, GuardarRenovacionRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarRenovacionCommand(token, r), ct));

    [HttpPost("renovaciones/{token:guid}/requerimiento")]
    public async Task<IActionResult> Requerimiento(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EnviarRequerimientoCommand(token), ct));

    [HttpPost("renovaciones/{token:guid}/renovar")]
    public async Task<IActionResult> Renovar(Guid token, RenovarRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new RenovarServicioCommand(token, r.NuevoVence), ct));

    [HttpPost("renovaciones/{token:guid}/retirar")]
    public async Task<IActionResult> RetirarRenovacion(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new RetirarRenovacionCommand(token), ct));

    // ---------- Solicitudes de compra ----------
    [HttpGet("solicitudes")]
    public async Task<IActionResult> Solicitudes(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarSolicitudesQuery(), ct));

    [HttpGet("solicitudes/{token:guid}")]
    public async Task<IActionResult> Solicitud(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetalleSolicitudQuery(token), ct));

    [HttpPost("solicitudes")]
    public async Task<IActionResult> CrearSolicitud(GuardarSolicitudRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarSolicitudCommand(null, r), ct));

    [HttpPut("solicitudes/{token:guid}")]
    public async Task<IActionResult> EditarSolicitud(Guid token, GuardarSolicitudRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarSolicitudCommand(token, r), ct));

    [HttpPost("solicitudes/{token:guid}/resolver")]
    public async Task<IActionResult> ResolverSolicitud(Guid token, ResolverRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new ResolverSolicitudCommand(token, r), ct));

    [HttpPost("solicitudes/{token:guid}/anular")]
    public async Task<IActionResult> AnularSolicitud(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new AnularSolicitudCommand(token), ct));

    // ---------- Pagos ----------
    [HttpGet("pagos")]
    public async Task<IActionResult> Pagos(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarPagosQuery(), ct));

    [HttpGet("pagos/{token:guid}")]
    public async Task<IActionResult> Pago(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetallePagoQuery(token), ct));

    [HttpPost("pagos")]
    public async Task<IActionResult> CrearPago(GuardarPagoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarPagoCommand(null, r), ct));

    [HttpPut("pagos/{token:guid}")]
    public async Task<IActionResult> EditarPago(Guid token, GuardarPagoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarPagoCommand(token, r), ct));

    /// <summary>COM-04: marcarlo Pagado descuenta su distribución de las partidas.</summary>
    [HttpPost("pagos/{token:guid}/pagar")]
    public async Task<IActionResult> Pagar(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new PagarCommand(token), ct));

    [HttpPost("pagos/{token:guid}/anular")]
    public async Task<IActionResult> AnularPago(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new AnularPagoCommand(token), ct));

    // ---------- Documentos: contrato de la solicitud; facturas y distribución del pago ----------
    [HttpPost("{dueno:regex(^(solicitudes|pagos)$)}/{token:guid}/documentos")]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> SubirDocumento(string dueno, Guid token, IFormFile archivo, [FromForm] string tipo, CancellationToken ct)
    {
        if ((dueno == "solicitudes") != (tipo == "Contrato")) return Problem(statusCode: 422, title: "tipo", detail: "El contrato va en la solicitud; facturas y distribución, en el pago.");
        if (archivo.Length > ReglasPresupuesto.MaximoMBDocumento * 1024L * 1024L)
            return Problem(statusCode: 422, title: "tamano", detail: $"El archivo supera el máximo de {ReglasPresupuesto.MaximoMBDocumento} MB.");
        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        return this.ToActionResult(await dispatcher.Send(new SubirDocumentoCommand(token, tipo, archivo.FileName, ms.ToArray()), ct));
    }

    [HttpGet("documentos/{token:guid}")]
    public async Task<IActionResult> Documento(Guid token, CancellationToken ct)
    {
        var r = await dispatcher.Query(new DescargarDocumentoQuery(token), ct);
        return r.IsSuccess ? File(r.Value.Contenido, r.Value.TipoContenido, r.Value.Nombre) : this.ToActionResult(r);
    }

    [HttpDelete("documentos/{token:guid}")]
    public async Task<IActionResult> EliminarDocumento(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarDocumentoCommand(token), ct));
}
