using Microsoft.AspNetCore.Mvc;

namespace ProBank.Web.Areas.ComprasPagos.Controllers;

[Area("ComprasPagos")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => View();
}
