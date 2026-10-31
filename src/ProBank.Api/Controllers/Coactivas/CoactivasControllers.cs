using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Coactivas;
using ProBank.Contracts.Coactivas;

namespace ProBank.Api.Controllers.Coactivas;

/// <summary>Coactivas › Oficios (COA-02, COA-04) y catálogo de entidades.</summary>
[ApiController]
[Route("api/coactivas")]
public sealed class OficiosController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("entidades")]
    public async Task<IActionResult> Entidades(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarEntidadesQuery(), ct));

    [HttpPost("entidades")]
    public async Task<IActionResult> CrearEntidad(GuardarEntidadRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarEntidadCommand(null, r), ct));

    [HttpPut("entidades/{id:int}")]
    public async Task<IActionResult> EditarEntidad(int id, GuardarEntidadRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarEntidadCommand(id, r), ct));

    [HttpGet("oficios")]
    public async Task<IActionResult> Oficios(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarOficiosQuery(), ct));

    [HttpGet("oficios/{token:guid}")]
    public async Task<IActionResult> Oficio(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerOficioQuery(token), ct));

    [HttpPost("oficios")]
    public async Task<IActionResult> CrearOficio(GuardarOficioRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarOficioCommand(null, r), ct));

    [HttpPut("oficios/{token:guid}")]
    public async Task<IActionResult> EditarOficio(Guid token, GuardarOficioRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarOficioCommand(token, r), ct));

    [HttpPost("oficios/{token:guid}/anular")]
    public async Task<IActionResult> AnularOficio(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new AnularOficioCommand(token), ct));

    /// <summary>COA-01: agrega al oficio una coactiva nueva (bloqueo) o una orden de levantamiento o transferencia.</summary>
    [HttpPost("oficios/{token:guid}/ordenes")]
    public async Task<IActionResult> AgregarOrden(Guid token, AgregarOrdenRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new AgregarOrdenCommand(token, r), ct));

    /// <summary>COA-04: filas de la hoja «Coactivas» del formato; devuelve lo cargado y cada fila con error.</summary>
    [HttpPost("carga")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Carga(CargaMasivaRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new CargaMasivaCommand(r), ct));
}

/// <summary>Coactivas › Coactivas y Coactivas por cliente (COA-01, COA-03, COA-05).</summary>
[ApiController]
[Route("api/coactivas")]
public sealed class CoactivasController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("coactivas")]
    public async Task<IActionResult> Listar([FromQuery] string? identificacion, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new ListarCoactivasQuery(string.IsNullOrWhiteSpace(identificacion) ? null : identificacion), ct));

    [HttpGet("coactivas/{token:guid}")]
    public async Task<IActionResult> Obtener(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerCoactivaQuery(token), ct));

    [HttpPut("coactivas/{token:guid}")]
    public async Task<IActionResult> Actualizar(Guid token, ActualizarCoactivaRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new ActualizarCoactivaCommand(token, r), ct));

    [HttpPost("coactivas/{token:guid}/anular")]
    public async Task<IActionResult> Anular(Guid token, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new AnularCoactivaCommand(token), ct));

    [HttpPost("coactivas/{token:guid}/productos")]
    public async Task<IActionResult> Producto(Guid token, GuardarProductoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new GuardarProductoCommand(token, r), ct));

    [HttpPost("coactivas/{token:guid}/bloqueos")]
    public async Task<IActionResult> Bloquear(Guid token, RegistrarBloqueoRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new RegistrarBloqueoCommand(token, r), ct));

    /// <summary>Levantamiento o transferencia (según el tipo de la orden).</summary>
    [HttpPost("ordenes/ejecutar")]
    public async Task<IActionResult> Ejecutar(EjecutarOrdenRequest r, CancellationToken ct) => this.ToActionResult(await dispatcher.Send(new EjecutarOrdenCommand(r), ct));

    [HttpGet("clientes")]
    public async Task<IActionResult> Clientes(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarClientesQuery(), ct));

    [HttpGet("clientes/{identificacion}/productos")]
    public async Task<IActionResult> Productos(string identificacion, CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarProductosClienteQuery(identificacion), ct));
}
