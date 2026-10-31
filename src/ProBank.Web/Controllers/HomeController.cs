using Microsoft.AspNetCore.Mvc;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Controllers;

public sealed class HomeController(ProBankApi api) : Controller
{
    /// <summary>Inicio / Panel (mismo diseño del prototipo).</summary>
    public IActionResult Index() => View();

    [HttpGet] public async Task<IActionResult> Panel(CancellationToken ct) => ApiResult.Json(await api.Get("api/panel", ct));

    /// <summary>Opción del menú que todavía no tiene pantalla propia.</summary>
    public IActionResult Opcion(string clave) { ViewData["Opcion"] = clave; return View(); }

    public IActionResult Error() => View();
}
