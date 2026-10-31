using Microsoft.AspNetCore.Mvc;

namespace ProBank.Web.Areas.Versiones.Controllers;

[Area("Versiones")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => View();
}
