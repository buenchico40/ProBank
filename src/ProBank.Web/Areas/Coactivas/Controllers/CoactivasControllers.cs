using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Coactivas;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.Coactivas.Controllers;

[Area("Coactivas")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Oficios");
}

[Area("Coactivas")]
public sealed class OficiosController(ProBankApi api) : Controller
{
    public IActionResult Index() => View();

    [HttpGet] public async Task<IActionResult> Datos(CancellationToken ct) => ApiResult.Json(await api.Get("api/coactivas/oficios", ct));
    [HttpGet] public async Task<IActionResult> Detalle(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/coactivas/oficios/{token}", ct));
    [HttpGet] public async Task<IActionResult> Entidades(CancellationToken ct) => ApiResult.Json(await api.Get("api/coactivas/entidades", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(Guid? token, [FromBody] GuardarOficioRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post("api/coactivas/oficios", r, ct) : await api.Put($"api/coactivas/oficios/{token}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/coactivas/oficios/{token}/anular", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Orden(Guid token, [FromBody] AgregarOrdenRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"api/coactivas/oficios/{token}/ordenes", r, ct));

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Carga([FromBody] CargaMasivaRequest r, CancellationToken ct) => ApiResult.Json(await api.Post("api/coactivas/carga", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarEntidad(int? id, [FromBody] GuardarEntidadRequest r, CancellationToken ct) =>
        ApiResult.Json(id is null ? await api.Post("api/coactivas/entidades", r, ct) : await api.Put($"api/coactivas/entidades/{id}", r, ct));
}

[Area("Coactivas")]
public sealed class CoactivasController(ProBankApi api) : Controller
{
    public IActionResult Index() => View();

    [HttpGet] public async Task<IActionResult> Datos(string? identificacion, CancellationToken ct) =>
        ApiResult.Json(await api.Get($"api/coactivas/coactivas?identificacion={Uri.EscapeDataString(identificacion ?? "")}", ct));
    [HttpGet] public async Task<IActionResult> Detalle(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/coactivas/coactivas/{token}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(Guid token, [FromBody] ActualizarCoactivaRequest r, CancellationToken ct) => ApiResult.Json(await api.Put($"api/coactivas/coactivas/{token}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/coactivas/coactivas/{token}/anular", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Producto(Guid token, [FromBody] GuardarProductoRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"api/coactivas/coactivas/{token}/productos", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bloqueo(Guid token, [FromBody] RegistrarBloqueoRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"api/coactivas/coactivas/{token}/bloqueos", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ejecutar([FromBody] EjecutarOrdenRequest r, CancellationToken ct) => ApiResult.Json(await api.Post("api/coactivas/ordenes/ejecutar", r, ct));
}

[Area("Coactivas")]
public sealed class ClientesController(ProBankApi api) : Controller
{
    public IActionResult Index() => View();

    [HttpGet] public async Task<IActionResult> Datos(CancellationToken ct) => ApiResult.Json(await api.Get("api/coactivas/clientes", ct));
    [HttpGet] public async Task<IActionResult> Productos(string identificacion, CancellationToken ct) =>
        ApiResult.Json(await api.Get($"api/coactivas/clientes/{Uri.EscapeDataString(identificacion)}/productos", ct));
}
