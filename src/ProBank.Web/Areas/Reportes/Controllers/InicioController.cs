using Microsoft.AspNetCore.Mvc;

namespace ProBank.Web.Areas.Reportes.Controllers;

[Area("Reportes")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => View();
}
