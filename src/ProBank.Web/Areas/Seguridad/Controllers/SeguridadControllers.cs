using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Seguridad;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Areas.Seguridad.Controllers;

[Area("Seguridad")]
public sealed class InicioController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Usuarios");
}

[Area("Seguridad")]
public sealed class UsuariosController(ProBankApi api) : Controller
{
    public IActionResult Index() => View();

    [HttpGet] public async Task<IActionResult> Datos(CancellationToken ct) => ApiResult.Json(await api.Get("api/seguridad/usuarios", ct));
    [HttpGet] public async Task<IActionResult> Ingresos(Guid token, CancellationToken ct) => ApiResult.Json(await api.Get($"api/seguridad/usuarios/{token}/ingresos", ct));
    [HttpGet] public async Task<IActionResult> Roles(CancellationToken ct) => ApiResult.Json(await api.Get("api/seguridad/roles", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest r, CancellationToken ct) => ApiResult.Json(await api.Post("api/seguridad/usuarios", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(Guid token, [FromBody] ActualizarUsuarioRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Put($"api/seguridad/usuarios/{token}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerClave(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/seguridad/usuarios/{token}/clave", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Desbloquear(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/seguridad/usuarios/{token}/desbloquear", new { }, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Revocar(Guid token, CancellationToken ct) => ApiResult.Json(await api.Post($"api/seguridad/usuarios/{token}/revocar", new { }, ct));
}

[Area("Seguridad")]
public sealed class RolesController(ProBankApi api) : Controller
{
    public IActionResult Index() => View();

    [HttpGet] public async Task<IActionResult> Datos(CancellationToken ct) => ApiResult.Json(await api.Get("api/seguridad/roles", ct));
    [HttpGet] public async Task<IActionResult> Permisos(int id, CancellationToken ct) => ApiResult.Json(await api.Get($"api/seguridad/roles/{id}/permisos", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(int? id, [FromBody] GuardarRolRequest r, CancellationToken ct) =>
        ApiResult.Json(id is null ? await api.Post("api/seguridad/roles", r, ct) : await api.Put($"api/seguridad/roles/{id}", r, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarPermisos(int id, [FromBody] GuardarPermisosRequest r, CancellationToken ct) =>
        ApiResult.Json(await api.Put($"api/seguridad/roles/{id}/permisos", r, ct));
}

[Area("Seguridad")]
public sealed class AuditoriaController(ProBankApi api) : Controller
{
    public IActionResult Index() => View();

    [HttpGet] public async Task<IActionResult> Datos(DateOnly? desde, DateOnly? hasta, string? modulo, CancellationToken ct) =>
        ApiResult.Json(await api.Get($"api/seguridad/auditoria?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&modulo={Uri.EscapeDataString(modulo ?? "")}", ct));
}

[Area("Seguridad")]
public sealed class ParametrosController(ProBankApi api) : Controller
{
    public IActionResult Index() => View();

    [HttpGet] public async Task<IActionResult> Datos(CancellationToken ct) => ApiResult.Json(await api.Get("api/seguridad/parametros", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([FromBody] GuardarParametrosRequest r, CancellationToken ct) => ApiResult.Json(await api.Put("api/seguridad/parametros", r, ct));
}

/// <summary>Perfil del usuario conectado: datos para la cabecera y el menú, cambio de rol (SEG-01) y de contraseña.</summary>
[Area("Seguridad")]
public sealed class PerfilController(ProBankApi api) : Controller
{
    [HttpGet] public async Task<IActionResult> Datos(CancellationToken ct) => ApiResult.Json(await api.Get("api/seguridad/perfil", ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol([FromBody] CambiarRolRequest r, CancellationToken ct)
    {
        if (!User.HasClaim("roles", r.Rol)) return Problem(statusCode: 403, title: "forbidden", detail: "El rol no está asignado a tu usuario.");
        var res = await api.Post("api/seguridad/perfil/rol", r, ct);
        if (res.Status == 200)
            Response.Cookies.Append("probank_rol", r.Rol, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
        return ApiResult.Json(res);
    }

    /// <summary>Al cambiar la clave la API revoca las sesiones: se cierra la de la Web y se vuelve a ingresar.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarClave([FromBody] CambiarClaveRequest r, CancellationToken ct)
    {
        var res = await api.Post("api/seguridad/perfil/clave", r, ct);
        if (res.Status != 200) return ApiResult.Json(res);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("probank_rol");
        return Json(new { redirect = "/Cuenta/Ingresar?aviso=clave" });
    }
}
