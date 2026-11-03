using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Marketing;
using ProBank.Contracts.Seguridad;
using ProBank.Web.ApiClients;
using ProBank.Web.Infrastructure;

namespace ProBank.Web.Controllers;

/// <summary>SEG-03: inicio y cierre de sesión con usuario y contraseña.</summary>
[AllowAnonymous]
public sealed class CuentaController(ProBankAuthApi auth, ProBankApi api, ILogger<CuentaController> log) : Controller
{
    private static readonly Dictionary<string, string> Avisos = new()
    {
        ["clave"] = "Contraseña cambiada. Ingresa con la nueva.",
        ["sesion"] = "Tu sesión terminó o fue revocada. Ingresa de nuevo.",
    };

    [HttpGet]
    public async Task<IActionResult> Ingresar(string? returnUrl, string? aviso, CancellationToken ct) =>
        await Vista(new IngresoVista { ReturnUrl = returnUrl, Aviso = aviso is not null ? Avisos.GetValueOrDefault(aviso) : null }, ct);

    /// <summary>MKT-01: imagen de una promoción vigente de esta pantalla (la API solo entrega las de «Inicio de sesión» para todos).</summary>
    [HttpGet, ResponseCache(Duration = 300)]
    public Task<IActionResult> Promocion(Guid token, CancellationToken ct) => ArchivoApi.Pasar(this, api, $"api/marketing/inicio-sesion/{token}/imagen", ct);

    // MKT-01: imágenes vigentes de la pantalla de inicio de sesión; si la API no responde, la pantalla sigue sin ellas
    private async Task<IActionResult> Vista(IngresoVista m, CancellationToken ct)
    {
        try
        {
            var (status, json) = await api.Get("api/marketing/inicio-sesion", ct);
            if (status == 200) m.Promociones = JsonSerializer.Deserialize<List<PromocionInicioDto>>(json, JsonSerializerOptions.Web) ?? [];
        }
        catch (HttpRequestException e) { log.LogWarning(e, "No se pudieron cargar las imágenes del inicio de sesión"); }
        return View("Ingresar", m);
    }

    /// <summary>La API rechazó el token (vencido, revocado o con otra IP): se cierra la sesión de la Web.</summary>
    [HttpGet]
    public async Task<IActionResult> Expirada()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("probank_rol");
        return RedirectToAction(nameof(Ingresar), new { aviso = "sesion" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(IngresoVista m, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(m.Usuario) || string.IsNullOrEmpty(m.Clave))
        {
            m.Error = "Ingresa usuario y contraseña.";
            return await Vista(m, ct);
        }

        var (sesion, error) = await auth.Ingresar(new IngresarRequest(m.Usuario.Trim(), m.Clave, IpCliente.De(HttpContext)), ct);
        if (sesion is null)
        {
            m.Error = error;
            m.Clave = null;
            return await Vista(m, ct);
        }

        var claims = new List<Claim> { new("name", sesion.Nombre), new("preferred_username", m.Usuario.Trim()) };
        claims.AddRange(sesion.Roles.Select(r => new Claim("roles", r)));
        if (sesion.DebeCambiarClave) claims.Add(new Claim("cambiar_clave", "1"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "name", "roles"));
        var props = new AuthenticationProperties { IsPersistent = false, ExpiresUtc = sesion.Expira };
        props.StoreTokens([new AuthenticationToken { Name = "access_token", Value = sesion.Token }]);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        // SEG-01: el rol predeterminado queda como activo hasta que el usuario elija otro desde su perfil.
        Response.Cookies.Append("probank_rol", sesion.RolPredeterminado ?? sesion.Roles.FirstOrDefault() ?? "Consulta",
            new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
        return LocalRedirect(Url.IsLocalUrl(m.ReturnUrl) ? m.ReturnUrl! : "/");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("probank_rol");
        return RedirectToAction(nameof(Ingresar));
    }
}

public sealed class IngresoVista
{
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
    public string? Aviso { get; set; }
    public IReadOnlyList<PromocionInicioDto> Promociones { get; set; } = [];
}
