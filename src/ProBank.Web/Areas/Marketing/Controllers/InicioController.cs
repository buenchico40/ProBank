using Microsoft.AspNetCore.Mvc;

namespace ProBank.Web.Areas.Marketing.Controllers;

[Area("Marketing")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => View();
}
