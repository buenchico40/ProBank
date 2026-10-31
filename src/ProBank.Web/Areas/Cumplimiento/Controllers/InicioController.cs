using Microsoft.AspNetCore.Mvc;

namespace ProBank.Web.Areas.Cumplimiento.Controllers;

[Area("Cumplimiento")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => View();
}
