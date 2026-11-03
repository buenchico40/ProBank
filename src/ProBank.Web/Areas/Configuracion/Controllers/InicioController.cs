using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Configuracion;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.Configuracion.Controllers;

[Area("Configuracion")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Parametros");
}

[Area("Configuracion")] public sealed class ParametrosController : Controller { public IActionResult Index() => View(); }

/// <summary>Puente de la Web hacia api/configuracion (con el token de la sesión y el rol activo).</summary>
[Area("Configuracion")]
public sealed class DatosController(ProBankApi api) : Controller
{
    [HttpGet] public async Task<IActionResult> Parametros(CancellationToken ct) => ApiResult.Json(await api.Get("api/configuracion/parametros", ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Guardar([FromBody] GuardarParametrosGlobalesRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Put("api/configuracion/parametros", r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ProbarCorreo([FromBody] ProbarCorreoRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Post("api/configuracion/correo/probar", r, ct));
}
