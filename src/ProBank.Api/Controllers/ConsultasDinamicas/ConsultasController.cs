using Microsoft.AspNetCore.Mvc;
using ProBank.Api.Infrastructure;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Contracts.ConsultasDinamicas;

namespace ProBank.Api.Controllers.ConsultasDinamicas;

[ApiController]
[Route("api/consultas")]
public sealed class ConsultasController(IDispatcher dispatcher) : ControllerBase
{
    /// <summary>SQL-01..06: ejecuta (o deja pendiente de aprobación) una consulta publicada.</summary>
    [HttpPost("ejecutar")]
    public async Task<IActionResult> Ejecutar(EjecutarConsultaRequest request, CancellationToken ct) =>
        this.ToActionResult(await dispatcher.Send(new EjecutarConsultaCommand(request), ct));
}
