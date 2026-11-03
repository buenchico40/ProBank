using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Marketing;
using ProBank.Contracts.Marketing;
using ProBank.Domain.Modules.Marketing;

namespace ProBank.Api.Controllers.Marketing;

/// <summary>Marketing: imágenes promocionales (MKT-01) y comunicados (MKT-02).</summary>
[ApiController]
[Route("api/marketing")]
public sealed class MarketingController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("audiencias")]
    public async Task<IActionResult> Audiencias(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new AudienciasQuery(), ct));

    // ---------- Imágenes promocionales ----------
    [HttpGet("promociones")]
    public async Task<IActionResult> Promociones(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarPromocionesQuery(), ct));

    [HttpGet("promociones/{token:guid}")]
    public async Task<IActionResult> Promocion(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerPromocionQuery(token), ct));

    [HttpPost("promociones")]
    public async Task<IActionResult> CrearPromocion(GuardarPromocionRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarPromocionCommand(null, r), ct));

    [HttpPut("promociones/{token:guid}")]
    public async Task<IActionResult> EditarPromocion(Guid token, GuardarPromocionRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarPromocionCommand(token, r), ct));

    /// <summary>JPG o PNG (máximo 2 MB; se recomienda 1200 × 300 px).</summary>
    [HttpPost("promociones/{token:guid}/imagen")]
    [RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> SubirImagen(Guid token, IFormFile archivo, CancellationToken ct)
    {
        if (archivo.Length > ReglasMarketing.MaximoMBImagen * 1024L * 1024L)
            return Problem(statusCode: 422, title: "tamano", detail: $"La imagen supera el máximo de {ReglasMarketing.MaximoMBImagen} MB.");
        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        return this.ToActionResult(await dispatcher.Send(new SubirImagenCommand(token, archivo.FileName, ms.ToArray()), ct));
    }

    [HttpGet("promociones/{token:guid}/imagen")]
    public async Task<IActionResult> Imagen(Guid token, CancellationToken ct) => Archivo(await dispatcher.Query(new ImagenPromocionQuery(token), ct));

    [HttpDelete("promociones/{token:guid}")]
    public async Task<IActionResult> EliminarPromocion(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarPromocionCommand(token), ct));

    [HttpPost("promociones/{token:guid}/publicar")]
    public async Task<IActionResult> PublicarPromocion(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new PublicarPromocionCommand(token), ct));

    [HttpPost("promociones/{token:guid}/retirar")]
    public async Task<IActionResult> RetirarPromocion(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new RetirarPromocionCommand(token), ct));

    /// <summary>Pantalla de inicio de sesión (sin sesión): solo lo vigente con esa ubicación y para todos.</summary>
    [HttpGet("inicio-sesion"), AllowAnonymous]
    public async Task<IActionResult> InicioSesion(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new PromocionesInicioQuery(), ct));

    [HttpGet("inicio-sesion/{token:guid}/imagen"), AllowAnonymous]
    public async Task<IActionResult> ImagenInicio(Guid token, CancellationToken ct) => Archivo(await dispatcher.Query(new ImagenInicioQuery(token), ct));

    // ---------- Comunicados ----------
    [HttpGet("comunicados")]
    public async Task<IActionResult> Comunicados(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarComunicadosQuery(), ct));

    [HttpGet("comunicados/{token:guid}")]
    public async Task<IActionResult> Comunicado(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerComunicadoQuery(token), ct));

    [HttpPost("comunicados")]
    public async Task<IActionResult> CrearComunicado(GuardarComunicadoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarComunicadoCommand(null, r), ct));

    [HttpPut("comunicados/{token:guid}")]
    public async Task<IActionResult> EditarComunicado(Guid token, GuardarComunicadoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarComunicadoCommand(token, r), ct));

    [HttpDelete("comunicados/{token:guid}")]
    public async Task<IActionResult> EliminarComunicado(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EliminarComunicadoCommand(token), ct));

    [HttpPost("comunicados/{token:guid}/publicar")]
    public async Task<IActionResult> PublicarComunicado(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new PublicarComunicadoCommand(token), ct));

    [HttpPost("comunicados/{token:guid}/retirar")]
    public async Task<IActionResult> RetirarComunicado(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new RetirarComunicadoCommand(token), ct));

    private IActionResult Archivo(Domain.Common.Result<ArchivoImagen> r) => r.IsSuccess ? File(r.Value.Contenido, r.Value.Tipo) : this.ToActionResult(r);
}
