using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Catalogos;

namespace ProBank.Api.Controllers;

[ApiController]
[Route("api/catalogos")]
public sealed class CatalogosController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("items")]
    public async Task<IActionResult> Items([FromQuery] bool abiertos = true, CancellationToken ct = default) =>
        this.ToActionResult(await dispatcher.Query(new ListarItemsQuery(abiertos), ct));

    /// <summary>Usuarios activos: técnicos de cualquier área y aprobadores.</summary>
    [HttpGet("usuarios")]
    public async Task<IActionResult> Usuarios(CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Query(new ListarUsuariosQuery(), ct));
}
