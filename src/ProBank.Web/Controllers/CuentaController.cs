using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProBank.Contracts.Seguridad;
using ProBank.Web.ApiClients;

namespace ProBank.Web.Controllers;

/// <summary>SEG-03: inicio y cierre de sesión con usuario y contraseña.</summary>
[AllowAnonymous]
public sealed class CuentaController(ProBankAuthApi auth) : Controller
{
    private static readonly Dictionary<string, string> Avisos = new()
    {
        ["clave"] = "Contraseña cambiada. Ingresa con la nueva.",
        ["sesion"] = "Tu sesión terminó o fue revocada. Ingresa de nuevo.",
    };

    [HttpGet]
    public IActionResult Ingresar(string? returnUrl, string? aviso) =>
        View(new IngresoVista { ReturnUrl = returnUrl, Aviso = aviso is not null ? Avisos.GetValueOrDefault(aviso) : null });

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
            return View(m);
        }

        var (sesion, error) = await auth.Ingresar(new IngresarRequest(m.Usuario.Trim(), m.Clave, IpCliente.De(HttpContext)), ct);
        if (sesion is null)
        {
            m.Error = error;
            m.Clave = null;
            return View(m);
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
}
