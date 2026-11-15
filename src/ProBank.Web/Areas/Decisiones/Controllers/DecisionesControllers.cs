using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Decisiones;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.Decisiones.Controllers;

[Area("Decisiones")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Registro");
}

// Una página por opción del menú; los datos van por DatosController.
[Area("Decisiones")] public sealed class RegistroController : Controller { public IActionResult Index() => View(); }
[Area("Decisiones")] public sealed class PendientesController : Controller { public IActionResult Index() => View(); }
[Area("Decisiones")] public sealed class TiposController : Controller { public IActionResult Index() => View(); }

/// <summary>Puente de la Web hacia api/decisiones (con el token de la sesión y el rol activo).</summary>
[Area("Decisiones")]
public sealed class DatosController(ProBankApi api) : Controller
{
    private const string B = "api/decisiones";

    [HttpGet] public async Task<IActionResult> Usuarios(CancellationToken ct) => ApiResult.Json(await api.Get("api/catalogos/usuarios", ct));

    // Tipos de decisión
    [HttpGet] public async Task<IActionResult> Tipos(bool soloActivos, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}/tipos?soloActivos={soloActivos}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarTipo(string? codigo, [FromBody] GuardarTipoDecisionRequest r, CancellationToken ct) =>
        ApiResult.Json(string.IsNullOrEmpty(codigo) ? await api.Post($"{B}/tipos", r, ct) : await api.Put($"{B}/tipos/{Uri.EscapeDataString(codigo)}", r, ct));

    // Decisiones
    [HttpGet] public async Task<IActionResult> Decisiones(string? alcance, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}?alcance={Uri.EscapeDataString(alcance ?? "todas")}", ct));
    [HttpGet] public async Task<IActionResult> Decision(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}/{token}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(Guid? token, [FromBody] GuardarDecisionRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post(B, r, ct) : await api.Put($"{B}/{token}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}/{token}/enviar", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Firmar(Guid token, [FromBody] FirmarDecisionRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}/{token}/firmar", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Modificar(Guid token, [FromBody] ModificarDecisionRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}/{token}/modificar", r, ct));

    /// <summary>rol: aprobadores o notificados.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarParticipante(Guid token, string rol, [FromBody] ParticipanteRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Post($"{B}/{token}/{Rol(rol)}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarParticipante(Guid token, string rol, int usuarioId, CancellationToken ct) =>
        ApiResult.Json(await api.Delete($"{B}/{token}/{Rol(rol)}/{usuarioId}", ct));

    private static string Rol(string rol) => rol == "aprobadores" ? "aprobadores" : "notificados";

    // DEC-08: archivos de respaldo
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(22_000_000)]
    public async Task<IActionResult> SubirArchivo(Guid token, IFormFile archivo, CancellationToken ct) =>
        ApiResult.Json(await api.PostArchivo($"{B}/{token}/archivos", archivo, new Dictionary<string, string?>(), ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarArchivo(Guid token, CancellationToken ct) => ApiResult.Json(await api.Delete($"{B}/archivos/{token}", ct));

    [HttpGet] public Task<IActionResult> Archivo(Guid token, CancellationToken ct) => ArchivoApi.Pasar(this, api, $"{B}/archivos/{token}", ct, descargar: true);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(Guid token, [FromBody] AnularDecisionRequest r, CancellationToken ct) => ApiResult.Json(await api.Post($"{B}/{token}/anular", r, ct));

    /// <summary>DEC-04: PDF firmado; con descargar=true se baja como archivo, si no se abre en el navegador.</summary>
    [HttpGet] public Task<IActionResult> Pdf(Guid token, bool descargar, CancellationToken ct) => ArchivoApi.Pasar(this, api, $"{B}/{token}/pdf", ct, descargar);
}
