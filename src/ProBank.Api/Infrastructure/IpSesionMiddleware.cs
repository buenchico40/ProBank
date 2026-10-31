namespace ProBank.Api.Infrastructure;

/// <summary>
/// SEG-04: si la sesión aparece desde otra IP, se rechaza la petición (el Front-end obliga a iniciar sesión de nuevo).
/// La IP del inicio de sesión viaja en el claim "ipaddr" del token; la Web reenvía la IP del usuario en X-Forwarded-For.
/// </summary>
internal sealed class IpSesionMiddleware(RequestDelegate next, ILogger<IpSesionMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var ipToken = ctx.User.FindFirst("ipaddr")?.Value;
        var ipActual = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? ctx.Connection.RemoteIpAddress?.ToString();
        if (ctx.User.Identity?.IsAuthenticated == true && ipToken is not null && ipActual is not null && ipToken != ipActual)
        {
            log.LogWarning("Sesión revocada: IP {Actual} distinta de {Token}", ipActual, ipToken);
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { code = "ip_distinta", message = "La sesión se revocó porque cambió la IP." });
            return;
        }
        await next(ctx);
    }
}
