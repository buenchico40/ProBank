using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Decisiones;
using ProBank.Contracts.Decisiones;
using ProBank.Domain.Modules.Decisiones;

namespace ProBank.Api.Controllers.Decisiones;

/// <summary>Decisiones del banco con sus aprobaciones firmadas y el PDF firmado (DEC-01 a DEC-04).</summary>
[ApiController]
[Route("api/decisiones")]
public sealed class DecisionesController(IDispatcher dispatcher) : ControllerBase
{
    // ---------- Tipos de decisión ----------
    [HttpGet("tipos")]
    public async Task<IActionResult> Tipos([FromQuery] bool soloActivos, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new ListarTiposDecisionQuery(soloActivos), ct));

    [HttpPost("tipos")]
    public async Task<IActionResult> CrearTipo(GuardarTipoDecisionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarTipoDecisionCommand(null, r), ct));

    [HttpPut("tipos/{codigo}")]
    public async Task<IActionResult> EditarTipo(string codigo, GuardarTipoDecisionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarTipoDecisionCommand(codigo, r), ct));

    // ---------- Decisiones ----------
    /// <param name="alcance">todas, pendientes (esperan mi firma) o notificadas.</param>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? alcance, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new ListarDecisionesQuery(alcance ?? "todas"), ct));

    [HttpGet("{token:guid}")]
    public async Task<IActionResult> Detalle(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new DetalleDecisionQuery(token), ct));

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarDecisionRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarDecisionCommand(null, r), ct));

    [HttpPut("{token:guid}")]
    public async Task<IActionResult> Editar(Guid token, GuardarDecisionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarDecisionCommand(token, r), ct));

    [HttpPost("{token:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EnviarDecisionCommand(token), ct));

    /// <summary>DEC-03: aprobar o rechazar con la clave del aprobador.</summary>
    [HttpPost("{token:guid}/firmar")]
    public async Task<IActionResult> Firmar(Guid token, FirmarDecisionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new FirmarDecisionCommand(token, r), ct));

    /// <summary>DEC-07: el aprobador corrige el contenido con su clave; nace una versión nueva y todos vuelven a firmar.</summary>
    [HttpPost("{token:guid}/modificar")]
    public async Task<IActionResult> Modificar(Guid token, ModificarDecisionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new ModificarDecisionCommand(token, r), ct));

    /// <summary>DEC-03 y DEC-05: {rol} aprobadores o notificados; agregar con POST, quitar con DELETE.</summary>
    [HttpPost("{token:guid}/{rol:regex(^(aprobadores|notificados)$)}")]
    public async Task<IActionResult> Agregar(Guid token, string rol, ParticipanteRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new CambiarParticipanteCommand(token, Rol(rol), true, r), ct));

    [HttpDelete("{token:guid}/{rol:regex(^(aprobadores|notificados)$)}/{usuarioId:int}")]
    public async Task<IActionResult> Quitar(Guid token, string rol, int usuarioId, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new CambiarParticipanteCommand(token, Rol(rol), false, new ParticipanteRequest(usuarioId, null)), ct));

    /// <summary>DEC-08: archivo de respaldo (en borrador); se verifica su contenido y se guarda su huella SHA-256.</summary>
    [HttpPost("{token:guid}/archivos")]
    [RequestSizeLimit(22_000_000)]
    public async Task<IActionResult> SubirArchivo(Guid token, IFormFile archivo, CancellationToken ct)
    {
        if (archivo.Length > ReglasDecisiones.MaximoMBArchivo * 1024L * 1024L)
            return Problem(statusCode: 422, title: "tamano", detail: $"El archivo supera el máximo de {ReglasDecisiones.MaximoMBArchivo} MB.");
        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        return this.ToActionResult(await dispatcher.Send(new SubirArchivoDecisionCommand(token, archivo.FileName, ms.ToArray()), ct));
    }

    [HttpDelete("archivos/{token:guid}")]
    public async Task<IActionResult> QuitarArchivo(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarArchivoDecisionCommand(token), ct));

    [HttpGet("archivos/{token:guid}")]
    public async Task<IActionResult> Archivo(Guid token, CancellationToken ct)
    {
        var r = await dispatcher.Query(new DescargarArchivoDecisionQuery(token), ct);
        return r.IsSuccess ? File(r.Value.Contenido, r.Value.TipoContenido, r.Value.Nombre) : this.ToActionResult(r);
    }

    [HttpPost("{token:guid}/anular")]
    public async Task<IActionResult> Anular(Guid token, AnularDecisionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new AnularDecisionCommand(token, r), ct));

    /// <summary>DEC-04: PDF con el detalle y las firmas, firmado con el certificado del banco.</summary>
    [HttpGet("{token:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid token, CancellationToken ct)
    {
        var r = await dispatcher.Query(new PdfDecisionQuery(token), ct);
        return r.IsSuccess ? File(r.Value.Contenido, r.Value.TipoContenido, r.Value.Nombre) : this.ToActionResult(r);
    }

    private static string Rol(string rol) => rol == "aprobadores" ? "Aprobador" : "Notificado";
}
