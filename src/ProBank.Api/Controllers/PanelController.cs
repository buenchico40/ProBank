using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Panel;

namespace ProBank.Api.Controllers;

[ApiController]
[Route("api/panel")]
public sealed class PanelController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ObtenerPanelQuery(), ct));
}
