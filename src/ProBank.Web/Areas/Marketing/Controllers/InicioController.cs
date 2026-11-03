using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Marketing;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.Marketing.Controllers;

[Area("Marketing")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Promociones");
}

// Una página por opción del menú (mismo diseño del prototipo); los datos van por DatosController.
[Area("Marketing")] public sealed class PromocionesController : Controller { public IActionResult Index() => View(); }
[Area("Marketing")] public sealed class ComunicadosController : Controller { public IActionResult Index() => View(); }

/// <summary>Puente de la Web hacia api/marketing (con el token de la sesión y el rol activo).</summary>
[Area("Marketing")]
public sealed class DatosController(ProBankApi api) : Controller
{
    [HttpGet] public async Task<IActionResult> Audiencias(CancellationToken ct) => ApiResult.Json(await api.Get("api/marketing/audiencias", ct));

    // Imágenes promocionales
    [HttpGet] public async Task<IActionResult> Promociones(CancellationToken ct) => ApiResult.Json(await api.Get("api/marketing/promociones", ct));
    [HttpGet] public async Task<IActionResult> Promocion(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/marketing/promociones/{token}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarPromocion(Guid? token, [FromBody] GuardarPromocionRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post("api/marketing/promociones", r, ct) : await api.Put($"api/marketing/promociones/{token}", r, ct));

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> SubirImagen(Guid token, IFormFile archivo, CancellationToken ct) =>
        ApiResult.Json(await api.PostArchivo($"api/marketing/promociones/{token}/imagen", archivo, new Dictionary<string, string?>(), ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarPromocion(Guid token, CancellationToken ct) => ApiResult.Json(await api.Delete($"api/marketing/promociones/{token}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicarPromocion(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/marketing/promociones/{token}/publicar", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RetirarPromocion(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/marketing/promociones/{token}/retirar", new { }, ct));

    /// <summary>Imagen de una promoción para el usuario conectado (Panel, ficha y vista previa).</summary>
    [HttpGet, ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Imagen(Guid token, CancellationToken ct) => await ArchivoApi.Pasar(this, api, $"api/marketing/promociones/{token}/imagen", ct);

    // Comunicados
    [HttpGet] public async Task<IActionResult> Comunicados(CancellationToken ct) => ApiResult.Json(await api.Get("api/marketing/comunicados", ct));
    [HttpGet] public async Task<IActionResult> Comunicado(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/marketing/comunicados/{token}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarComunicado(Guid? token, [FromBody] GuardarComunicadoRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post("api/marketing/comunicados", r, ct) : await api.Put($"api/marketing/comunicados/{token}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarComunicado(Guid token, CancellationToken ct) => ApiResult.Json(await api.Delete($"api/marketing/comunicados/{token}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicarComunicado(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/marketing/comunicados/{token}/publicar", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RetirarComunicado(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/marketing/comunicados/{token}/retirar", new { }, ct));
}
