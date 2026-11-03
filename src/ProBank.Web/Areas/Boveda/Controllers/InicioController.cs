using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Boveda;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.Boveda.Controllers;

[Area("Boveda")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Grupos");
}

// Una página por opción del menú (mismo diseño del prototipo); los datos van por DatosController.
[Area("Boveda")] public sealed class GruposController : Controller { public IActionResult Index() => View(); }
[Area("Boveda")] public sealed class CertificadosController : Controller { public IActionResult Index() => View(); }
[Area("Boveda")] public sealed class CuentasController : Controller { public IActionResult Index() => View(); }
[Area("Boveda")] public sealed class SecretosController : Controller { public IActionResult Index() => View(); }

/// <summary>Puente de la Web hacia api/boveda (con el token de la sesión y el rol activo). {clase}: cuentas o secretos.</summary>
[Area("Boveda")]
public sealed class DatosController(ProBankApi api) : Controller
{
    [HttpGet] public async Task<IActionResult> Usuarios(CancellationToken ct) => ApiResult.Json(await api.Get("api/catalogos/usuarios", ct));

    // Grupos de acceso
    [HttpGet] public async Task<IActionResult> Grupos(CancellationToken ct) => ApiResult.Json(await api.Get("api/boveda/grupos", ct));
    [HttpGet] public async Task<IActionResult> Grupo(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/boveda/grupos/{token}", ct));
    [HttpGet] public async Task<IActionResult> DetalleGrupo(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/boveda/grupos/{token}/detalle", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarGrupo(Guid? token, [FromBody] GuardarGrupoRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post("api/boveda/grupos", r, ct) : await api.Put($"api/boveda/grupos/{token}", r, ct));

    // Certificados
    [HttpGet] public async Task<IActionResult> Certificados(CancellationToken ct) => ApiResult.Json(await api.Get("api/boveda/certificados", ct));
    [HttpGet] public async Task<IActionResult> Certificado(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/boveda/certificados/{token}", ct));
    [HttpGet] public async Task<IActionResult> Renovaciones(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/boveda/certificados/{token}/renovaciones", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarCertificado(Guid? token, [FromBody] GuardarCertificadoRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post("api/boveda/certificados", r, ct) : await api.Put($"api/boveda/certificados/{token}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RenovarCertificado(Guid token, [FromBody] RenovarCertificadoRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Post($"api/boveda/certificados/{token}/renovar", r, ct));

    // Cuentas técnicas y secretos
    [HttpGet] public async Task<IActionResult> Cuentas(CancellationToken ct) => ApiResult.Json(await api.Get("api/boveda/cuentas", ct));
    [HttpGet] public async Task<IActionResult> Cuenta(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/boveda/cuentas/{token}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarCuenta(Guid? token, [FromBody] GuardarCuentaRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post("api/boveda/cuentas", r, ct) : await api.Put($"api/boveda/cuentas/{token}", r, ct));

    [HttpGet] public async Task<IActionResult> Secretos(CancellationToken ct) => ApiResult.Json(await api.Get("api/boveda/secretos", ct));
    [HttpGet] public async Task<IActionResult> Secreto(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/boveda/secretos/{token}", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarSecreto(Guid? token, [FromBody] GuardarSecretoRequest r, CancellationToken ct) =>
        ApiResult.Json(token is null ? await api.Post("api/boveda/secretos", r, ct) : await api.Put($"api/boveda/secretos/{token}", r, ct));

    // Valor de la credencial: historial, mostrar y copiar (registrados) y cambiar
    [HttpGet] public async Task<IActionResult> Historial(string clase, Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/boveda/{Clase(clase)}/{token}/historial", ct));

    [HttpPost, ValidateAntiForgeryToken, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Mostrar(string clase, Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/boveda/{Clase(clase)}/{token}/mostrar", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Copiar(string clase, Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/boveda/{Clase(clase)}/{token}/copiar", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cambiar(string clase, Guid token, [FromBody] CambiarCredencialRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Post($"api/boveda/{Clase(clase)}/{token}/cambiar", r, ct));

    private static string Clase(string clase) => clase == "cuentas" ? "cuentas" : "secretos";
}
