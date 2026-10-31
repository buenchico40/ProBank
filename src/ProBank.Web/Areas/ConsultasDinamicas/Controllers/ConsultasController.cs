using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.ConsultasDinamicas.Controllers;

[Area("ConsultasDinamicas")]
public sealed class ConsultasController(ProBankApi api) : Controller
{
    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ejecutar([FromBody] EjecutarConsultaRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Post("api/consultas/ejecutar", r, ct));
}
