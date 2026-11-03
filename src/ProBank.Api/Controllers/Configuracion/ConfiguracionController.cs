using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Configuracion;
using ProBank.Contracts.Configuracion;

namespace ProBank.Api.Controllers.Configuracion;

/// <summary>Configuración global de la solución (CFG-01 a CFG-04): parámetros por categoría y prueba del correo.</summary>
[ApiController]
[Route("api/configuracion")]
public sealed class ConfiguracionController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("parametros")]
    public async Task<IActionResult> Parametros(CancellationToken ct) => this.ToActionResult(await dispatcher.Query(new ListarParametrosGlobalesQuery(), ct));

    /// <summary>Guarda los valores que cambian; se valida el conjunto completo (por ejemplo, el correo según su proveedor).</summary>
    [HttpPut("parametros")]
    public async Task<IActionResult> Guardar(GuardarParametrosGlobalesRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new GuardarParametrosGlobalesCommand(r), ct));

    /// <summary>CFG-03: envía un correo de prueba con la configuración guardada.</summary>
    [HttpPost("correo/probar")]
    public async Task<IActionResult> ProbarCorreo(ProbarCorreoRequest r, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new ProbarCorreoCommand(r.Destinatario), ct));
}
