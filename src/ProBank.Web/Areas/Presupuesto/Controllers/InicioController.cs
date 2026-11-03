using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Presupuesto;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.Presupuesto.Controllers;

[Area("Presupuesto")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Partidas");
}

// Una página por opción del menú (mismo diseño del prototipo); los datos van por DatosController.
[Area("Presupuesto")] public sealed class PartidasController : Controller { public IActionResult Index() => View(); }
[Area("Presupuesto")] public sealed class MovimientosController : Controller { public IActionResult Index() => View(); }
[Area("Presupuesto")] public sealed class RenovacionesController : Controller { public IActionResult Index() => View(); }
[Area("Presupuesto")] public sealed class SolicitudesController : Controller { public IActionResult Index() => View(); }
[Area("Presupuesto")] public sealed class PagosController : Controller { public IActionResult Index() => View(); }
[Area("Presupuesto")] public sealed class AreasController : Controller { public IActionResult Index() => View(); }

/// <summary>Puente de la Web hacia api/presupuesto (con el token de la sesión y el rol activo).</summary>
[Area("Presupuesto")]
public sealed class DatosController(ProBankApi api) : Controller
{
    private const string B = "api/presupuesto/";
    private Task<(int, string)> Guardar<T>(string recurso, Guid? token, T r, CancellationToken ct) =>
        token is null ? api.Post(B + recurso, r, ct) : api.Put($"{B}{recurso}/{token}", r, ct);

    // Áreas del presupuesto
    [HttpGet] public async Task<IActionResult> Areas(bool soloActivas, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}areas?soloActivas={soloActivas}", ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarArea(string? codigo, [FromBody] GuardarAreaRequest r, CancellationToken ct) =>
        ApiResult.Json(codigo is null ? await api.Post(B + "areas", r, ct) : await api.Put($"{B}areas/{Uri.EscapeDataString(codigo)}", r, ct));

    // Partidas ([FromQuery] porque «area» también es el valor de ruta del área MVC)
    [HttpGet] public async Task<IActionResult> Partidas(int? anio, [FromQuery] string? area, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}partidas?anio={anio}{(area is null ? "" : "&area=" + Uri.EscapeDataString(area))}", ct));
    [HttpGet] public async Task<IActionResult> Partida(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}partidas/{token}", ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarPartida(Guid? token, [FromBody] GuardarPartidaRequest r, CancellationToken ct) => ApiResult.Json(await Guardar("partidas", token, r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EstadoPartida(Guid token, string accion, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}partidas/{token}/{Uri.EscapeDataString(accion)}", new { }, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EliminarPartida(Guid token, CancellationToken ct) => ApiResult.Json(await api.Delete($"{B}partidas/{token}", ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Preparar([FromQuery] string? area, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}partidas/preparar?area={Uri.EscapeDataString(area ?? "")}", new { }, ct));
    [HttpGet] public Task<IActionResult> Reporte([FromQuery] string? area, int anio, bool descargar, CancellationToken ct) =>
        ArchivoApi.Pasar(this, api, $"{B}reporte?area={Uri.EscapeDataString(area ?? "")}&anio={anio}", ct, descargar);

    // Movimientos
    [HttpGet] public async Task<IActionResult> Movimientos(CancellationToken ct) => ApiResult.Json(await api.Get($"{B}movimientos", ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarMovimiento(Guid? token, [FromBody] GuardarMovimientoRequest r, CancellationToken ct) => ApiResult.Json(await Guardar("movimientos", token, r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ResolverMovimiento(Guid token, [FromBody] ResolverRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}movimientos/{token}/resolver", r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> AnularMovimiento(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}movimientos/{token}/anular", new { }, ct));

    // Renovaciones
    [HttpGet] public async Task<IActionResult> Renovaciones(CancellationToken ct) => ApiResult.Json(await api.Get($"{B}renovaciones", ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarRenovacion(Guid? token, [FromBody] GuardarRenovacionRequest r, CancellationToken ct) => ApiResult.Json(await Guardar("renovaciones", token, r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Requerimiento(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}renovaciones/{token}/requerimiento", new { }, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Renovar(Guid token, [FromBody] RenovarRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}renovaciones/{token}/renovar", r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> RetirarRenovacion(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}renovaciones/{token}/retirar", new { }, ct));

    // Solicitudes de compra
    [HttpGet] public async Task<IActionResult> Solicitudes(CancellationToken ct) => ApiResult.Json(await api.Get($"{B}solicitudes", ct));
    [HttpGet] public async Task<IActionResult> Solicitud(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}solicitudes/{token}", ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarSolicitud(Guid? token, [FromBody] GuardarSolicitudRequest r, CancellationToken ct) => ApiResult.Json(await Guardar("solicitudes", token, r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ResolverSolicitud(Guid token, [FromBody] ResolverRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}solicitudes/{token}/resolver", r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> AnularSolicitud(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}solicitudes/{token}/anular", new { }, ct));

    // Pagos
    [HttpGet] public async Task<IActionResult> Pagos(CancellationToken ct) => ApiResult.Json(await api.Get($"{B}pagos", ct));
    [HttpGet] public async Task<IActionResult> Pago(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}pagos/{token}", ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarPago(Guid? token, [FromBody] GuardarPagoRequest r, CancellationToken ct) => ApiResult.Json(await Guardar("pagos", token, r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Pagar(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}pagos/{token}/pagar", new { }, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> AnularPago(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}pagos/{token}/anular", new { }, ct));

    // Documentos: contrato de la solicitud (dueno=solicitudes); facturas y distribución del pago (dueno=pagos)
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> SubirDocumento(string dueno, Guid token, string tipo, IFormFile archivo, CancellationToken ct) =>
        ApiResult.Json(await api.PostArchivo($"{B}{(dueno == "pagos" ? "pagos" : "solicitudes")}/{token}/documentos", archivo, new Dictionary<string, string?> { ["tipo"] = tipo }, ct));
    [HttpGet] public Task<IActionResult> Documento(Guid token, CancellationToken ct) => ArchivoApi.Pasar(this, api, $"{B}documentos/{token}", ct, descargar: true);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EliminarDocumento(Guid token, CancellationToken ct) => ApiResult.Json(await api.Delete($"{B}documentos/{token}", ct));
}
