using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.ConsultasDinamicas.Controllers;

[Area("ConsultasDinamicas")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Catalogo");
}

// Una página por opción del menú (mismo diseño del prototipo); los datos van por DatosController.
[Area("ConsultasDinamicas")] public sealed class CatalogoController : Controller { public IActionResult Index() => View(); }
[Area("ConsultasDinamicas")] public sealed class EjecucionesController : Controller { public IActionResult Index() => View(); }
[Area("ConsultasDinamicas")] public sealed class AuditoriaController : Controller { public IActionResult Index() => View(); }
[Area("ConsultasDinamicas")] public sealed class ConexionesController : Controller { public IActionResult Index() => View(); }

/// <summary>Puente de la Web hacia api/consultas (con el token de la sesión y el rol activo).</summary>
[Area("ConsultasDinamicas")]
public sealed class DatosController(ProBankApi api) : Controller
{
    private const string B = "api/consultas/";
    private static string E(string s) => Uri.EscapeDataString(s);

    // Bases de datos
    [HttpGet] public async Task<IActionResult> Conexiones(CancellationToken ct) => ApiResult.Json(await api.Get(B + "conexiones", ct));
    [HttpGet] public async Task<IActionResult> Conexion(string codigo, CancellationToken ct) => ApiResult.Json(await api.Get(B + "conexiones/" + E(codigo), ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarConexion(string? codigo, [FromBody] GuardarConexionRequest r, CancellationToken ct) =>
        ApiResult.Json(codigo is null ? await api.Post(B + "conexiones", r, ct) : await api.Put(B + "conexiones/" + E(codigo), r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EliminarConexion(string codigo, CancellationToken ct) => ApiResult.Json(await api.Delete(B + "conexiones/" + E(codigo), ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ProbarConexion(string codigo, CancellationToken ct) => ApiResult.Json(await api.Post(B + "conexiones/" + E(codigo) + "/probar", new { }, ct));

    // Catálogo
    [HttpGet] public async Task<IActionResult> Catalogo(CancellationToken ct) => ApiResult.Json(await api.Get(B + "catalogo", ct));
    [HttpGet] public async Task<IActionResult> Opciones(CancellationToken ct) => ApiResult.Json(await api.Get(B + "catalogo/opciones", ct));
    [HttpGet] public async Task<IActionResult> Consulta(string codigo, CancellationToken ct) => ApiResult.Json(await api.Get(B + "catalogo/" + E(codigo), ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarConsulta(string? codigo, [FromBody] GuardarConsultaRequest r, CancellationToken ct) =>
        ApiResult.Json(codigo is null ? await api.Post(B + "catalogo", r, ct) : await api.Put(B + "catalogo/" + E(codigo), r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EliminarConsulta(string codigo, CancellationToken ct) => ApiResult.Json(await api.Delete(B + "catalogo/" + E(codigo), ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> LeerDefinicion(string codigo, string conexion, CancellationToken ct) =>
        ApiResult.Json(await api.Post($"{B}catalogo/{E(codigo)}/definicion?conexion={E(conexion)}", new { }, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarParametros(string codigo, [FromBody] GuardarParametrosRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Put($"{B}catalogo/{E(codigo)}/parametros", r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EstadoConsulta(string codigo, string accion, CancellationToken ct) =>
        ApiResult.Json(await api.Post($"{B}catalogo/{E(codigo)}/{E(accion)}", new { }, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> GuardarProgramacion(string consulta, string? codigo, [FromBody] GuardarProgramacionRequest r, CancellationToken ct) =>
        ApiResult.Json(codigo is null ? await api.Post($"{B}catalogo/{E(consulta)}/programaciones", r, ct) : await api.Put($"{B}catalogo/{E(consulta)}/programaciones/{E(codigo)}", r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EliminarProgramacion(string codigo, CancellationToken ct) => ApiResult.Json(await api.Delete(B + "programaciones/" + E(codigo), ct));

    // Ejecución, aprobaciones y resultados
    [HttpGet] public async Task<IActionResult> Disponibles(CancellationToken ct) => ApiResult.Json(await api.Get(B + "disponibles", ct));
    [HttpGet] public async Task<IActionResult> ParaEjecutar(string codigo, CancellationToken ct) => ApiResult.Json(await api.Get(B + "disponibles/" + E(codigo), ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Ejecutar([FromBody] EjecutarConsultaRequest r, CancellationToken ct) => ApiResult.Json(await api.Post(B + "ejecutar", r, ct));
    [HttpGet] public async Task<IActionResult> Ejecuciones(CancellationToken ct) => ApiResult.Json(await api.Get(B + "ejecuciones", ct));
    [HttpGet] public async Task<IActionResult> Ejecucion(string codigo, CancellationToken ct) => ApiResult.Json(await api.Get(B + "ejecuciones/" + E(codigo), ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Resolver(string codigo, [FromBody] ResolverEjecucionRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Post($"{B}ejecuciones/{E(codigo)}/resolver", r, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> EjecutarAprobada(string codigo, CancellationToken ct) =>
        ApiResult.Json(await api.Post($"{B}ejecuciones/{E(codigo)}/ejecutar", new { }, ct));
    [HttpGet] public async Task<IActionResult> Resultado(string codigo, CancellationToken ct) => ApiResult.Json(await api.Get($"{B}ejecuciones/{E(codigo)}/resultado", ct));
    [HttpGet] public Task<IActionResult> Exportar(string codigo, string formato, CancellationToken ct) =>
        ArchivoApi.Pasar(this, api, $"{B}ejecuciones/{E(codigo)}/exportar?formato={E(formato)}", ct, descargar: true);

    // Auditoría de ejecuciones
    [HttpGet] public async Task<IActionResult> Auditoria(string desde, string hasta, CancellationToken ct) =>
        ApiResult.Json(await api.Get($"{B}auditoria?desde={E(desde)}&hasta={E(hasta)}", ct));
}
