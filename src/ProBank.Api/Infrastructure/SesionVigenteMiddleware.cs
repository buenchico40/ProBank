using System.Globalization;
using ProBank.Api.Seguridad;
using ProBank.Application.Abstractions;

namespace ProBank.Api.Infrastructure;

/// <summary>
/// SEG-07: el token deja de valer si su sello no coincide con el de la base (sesiones revocadas, clave cambiada, roles
/// o estado modificados) o si el usuario está inactivo. Con una clave temporal solo se permite el perfil (para cambiarla).
/// </summary>
internal sealed class SesionVigenteMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, ISesiones sesiones)
    {
        if (ctx.User.Identity?.IsAuthenticated == true
            && int.TryParse(ctx.User.FindFirst("probank_uid")?.Value, CultureInfo.InvariantCulture, out var uid))
        {
            var sello = ctx.User.FindFirst(EmisorTokens.ClaimSello)?.Value;
            var vigente = await sesiones.Obtener(uid, ctx.RequestAborted);
            if (vigente is null || vigente.Estado != "Activo" || sello != vigente.Sello.ToString(CultureInfo.InvariantCulture))
            {
                await Rechazar(ctx, StatusCodes.Status401Unauthorized, "sesion_revocada", "La sesión ya no es válida. Ingresa de nuevo.");
                return;
            }
            if (ctx.User.HasClaim(EmisorTokens.ClaimCambiarClave, "1")
                && !ctx.Request.Path.StartsWithSegments("/api/seguridad/perfil", StringComparison.OrdinalIgnoreCase))
            {
                await Rechazar(ctx, StatusCodes.Status403Forbidden, "cambiar_clave", "Debes cambiar tu contraseña temporal antes de continuar.");
                return;
            }
        }
        await next(ctx);
    }

    private static Task Rechazar(HttpContext ctx, int status, string codigo, string mensaje)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new { title = codigo, detail = mensaje, status }, options: null, contentType: "application/problem+json");
    }
}
