using Microsoft.AspNetCore.Mvc;

namespace ProBank.Web.Areas.Credenciales.Controllers;

[Area("Credenciales")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => View();
}
