using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Portafolio;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.Portafolio.Controllers;

// Una página por opción del menú de Portafolio (mismo diseño del prototipo); los datos van por DatosController.
[Area("Portafolio")] public sealed class ProyectosController : Controller { public IActionResult Index() => View(); }
[Area("Portafolio")] public sealed class CronogramaController : Controller { public IActionResult Index() => View(); }
[Area("Portafolio")] public sealed class TareasController : Controller { public IActionResult Index() => View(); }
[Area("Portafolio")] public sealed class AprobacionesController : Controller { public IActionResult Index() => View(); }
[Area("Portafolio")] public sealed class AcuerdosController : Controller { public IActionResult Index() => View(); }
[Area("Portafolio")] public sealed class InformesController : Controller { public IActionResult Index() => View(); }
[Area("Portafolio")] public sealed class ParametrizacionController : Controller { public IActionResult Index() => View(); }

/// <summary>Puente de la Web hacia api/portafolio (con el token de la sesión y el rol activo).</summary>
[Area("Portafolio")]
public sealed class DatosController(ProBankApi api) : Controller
{
    // Ítems
    [HttpGet] public async Task<IActionResult> Items(bool abiertos, CancellationToken ct) => ApiResult.Json(await api.Get($"api/portafolio/items?abiertos={abiertos}", ct));
    [HttpGet] public async Task<IActionResult> Item(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/portafolio/items/{token}", ct));
    [HttpGet] public async Task<IActionResult> Usuarios(CancellationToken ct) => ApiResult.Json(await api.Get("api/catalogos/usuarios", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarItem(Guid? token, [FromBody] GuardarItemRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post("api/portafolio/items", r, ct) : await api.Put($"api/portafolio/items/{token}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarItem(Guid token, CancellationToken ct) => ApiResult.Json(await api.Delete($"api/portafolio/items/{token}", ct));

    // Documentos adjuntos de un ítem (ambito=Item) o de una tarea (ambito=Tarea)
    private static string Dueno(string ambito) => ambito == "Tarea" ? "tareas" : "items";

    [HttpGet] public async Task<IActionResult> Adjuntos(string ambito, Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/portafolio/{Dueno(ambito)}/{token}/adjuntos", ct));

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(21_000_000)]
    public async Task<IActionResult> SubirAdjunto(string ambito, Guid token, IFormFile archivo, [FromForm] string? descripcion, CancellationToken ct) =>
        ApiResult.Json(await api.PostArchivo($"api/portafolio/{Dueno(ambito)}/{token}/adjuntos", archivo,
            new Dictionary<string, string?> { ["descripcion"] = descripcion }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarAdjunto(Guid token, [FromBody] ActualizarAdjuntoRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Put($"api/portafolio/adjuntos/{token}", r, ct));

    [HttpGet] public async Task<IActionResult> ParametrosAdjunto(CancellationToken ct) => ApiResult.Json(await api.Get("api/portafolio/adjuntos/parametros", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarParametroAdjunto(string ambito, [FromBody] GuardarParametroAdjuntoRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Put($"api/portafolio/adjuntos/parametros/{Uri.EscapeDataString(ambito)}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAdjunto(Guid token, CancellationToken ct) => ApiResult.Json(await api.Delete($"api/portafolio/adjuntos/{token}", ct));

    [HttpGet] public Task<IActionResult> Descargar(Guid token, CancellationToken ct) => ArchivoApi.Pasar(this, api, $"api/portafolio/adjuntos/{token}", ct, descargar: true);

    /// <summary>PDF del informe: en línea para verlo en la página o como descarga.</summary>
    [HttpGet] public Task<IActionResult> InformePdf(int id, bool descargar, CancellationToken ct) => ArchivoApi.Pasar(this, api, $"api/portafolio/informes/{id}/pdf", ct, descargar);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Hitos(Guid token, [FromBody] GuardarHitosRequest r, CancellationToken ct) => ApiResult.Json(await api.Put($"api/portafolio/items/{token}/hitos", r, ct));

    [HttpGet] public async Task<IActionResult> Cronograma(Guid? item, CancellationToken ct) => ApiResult.Json(await api.Get($"api/portafolio/cronograma?item={item}", ct));

    // Tareas y aprobaciones
    [HttpGet] public async Task<IActionResult> Tareas(Guid? item, bool todas, CancellationToken ct) => ApiResult.Json(await api.Get($"api/portafolio/tareas?item={item}&todas={todas}", ct));
    [HttpGet] public async Task<IActionResult> Tarea(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/portafolio/tareas/{token}", ct));
    [HttpGet] public async Task<IActionResult> Aprobaciones(CancellationToken ct) => ApiResult.Json(await api.Get("api/portafolio/aprobaciones", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTarea([FromBody] CrearTareaRequest r, CancellationToken ct) => ApiResult.Json(await api.Post("api/portafolio/tareas", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarTarea(Guid token, [FromBody] ActualizarTareaRequest r, CancellationToken ct) => ApiResult.Json(await api.Put($"api/portafolio/tareas/{token}", r, ct));

    /// <summary>enviar, completar, aprobar o rechazar.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AccionTarea(Guid token, string accion, [FromBody] RechazarRequest? r, CancellationToken ct) =>
        accion is "enviar" or "completar" or "aprobar" or "rechazar"
            ? ApiResult.Json(await api.Post($"api/portafolio/tareas/{token}/{accion}", (object?)r ?? new { }, ct))
            : BadRequest();

    // Acuerdos
    [HttpGet] public async Task<IActionResult> Acuerdos(CancellationToken ct) => ApiResult.Json(await api.Get("api/portafolio/acuerdos", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAcuerdos([FromBody] RegistrarAcuerdosRequest r, CancellationToken ct) => ApiResult.Json(await api.Post("api/portafolio/acuerdos", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarAcuerdo(int id, [FromBody] ActualizarAcuerdoRequest r, CancellationToken ct) => ApiResult.Json(await api.Put($"api/portafolio/acuerdos/{id}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAcuerdo(int id, CancellationToken ct) => ApiResult.Json(await api.Delete($"api/portafolio/acuerdos/{id}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CumplirAcuerdo(int id, CancellationToken ct) => ApiResult.Json(await api.Post($"api/portafolio/acuerdos/{id}/cumplir", new { }, ct));

    // Informes
    [HttpGet] public async Task<IActionResult> Informes(CancellationToken ct) => ApiResult.Json(await api.Get("api/portafolio/informes", ct));
    [HttpGet] public async Task<IActionResult> Informe(int id, CancellationToken ct) => ApiResult.Json(await api.Get($"api/portafolio/informes/{id}", ct));
    [HttpGet] public async Task<IActionResult> ResumenInforme(int id, CancellationToken ct) => ApiResult.Json(await api.Get($"api/portafolio/informes/{id}/resumen", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarInforme(int? id, [FromBody] GuardarInformeRequest r, CancellationToken ct) =>
        ApiResult.Json(id is null ? await api.Post("api/portafolio/informes", r, ct) : await api.Put($"api/portafolio/informes/{id}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarInforme(int id, CancellationToken ct) => ApiResult.Json(await api.Post($"api/portafolio/informes/{id}/generar", new { }, ct));

    // Parametrización (PRY-09)
    [HttpGet] public async Task<IActionResult> Parametrizacion(CancellationToken ct) => ApiResult.Json(await api.Get("api/portafolio/parametrizacion", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarAccion(string accion, [FromBody] GuardarAccionRolRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Put($"api/portafolio/parametrizacion/{Uri.EscapeDataString(accion)}", r, ct));
}
