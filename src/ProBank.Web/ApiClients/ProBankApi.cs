using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using ProBank.Contracts.Seguridad;

namespace ProBank.Web.ApiClients;

/// <summary>
/// Agrega a cada llamada a la API el token de la sesión, el rol activo y la IP del usuario
/// (la API la compara con la del inicio de sesión, SEG-04).
/// </summary>
public sealed class ApiTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = accessor.HttpContext;
        if (ctx is not null)
        {
            var token = await ctx.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new("Bearer", token);
            var rol = ctx.Request.Cookies["probank_rol"];
            if (!string.IsNullOrEmpty(rol)) request.Headers.Add("X-ProBank-Rol", rol);
            request.Headers.Add("X-Forwarded-For", IpCliente.De(ctx));
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

public static class IpCliente
{
    // En App Service la IP real llega por X-Forwarded-For (ASPNETCORE_FORWARDEDHEADERS_ENABLED=true).
    public static string De(HttpContext ctx) => ctx.Connection.RemoteIpAddress switch
    {
        null => "0.0.0.0",
        { IsIPv4MappedToIPv6: true } ip => ip.MapToIPv4().ToString(),
        var ip => ip.ToString(),
    };
}

/// <summary>Cliente tipado de ProBank.Api. Devuelve el cuerpo JSON tal cual para que la vista lo use.</summary>
public sealed class ProBankApi(HttpClient http)
{
    public async Task<(int Status, string Json)> Get(string ruta, CancellationToken ct)
    {
        using var r = await http.GetAsync(ruta, ct);
        return ((int)r.StatusCode, await r.Content.ReadAsStringAsync(ct));
    }

    public async Task<(int Status, string Json)> Post<T>(string ruta, T cuerpo, CancellationToken ct)
    {
        using var r = await http.PostAsJsonAsync(ruta, cuerpo, ct);
        return ((int)r.StatusCode, await r.Content.ReadAsStringAsync(ct));
    }

    public async Task<(int Status, string Json)> Delete(string ruta, CancellationToken ct)
    {
        using var r = await http.DeleteAsync(ruta, ct);
        return ((int)r.StatusCode, await r.Content.ReadAsStringAsync(ct));
    }

    public async Task<(int Status, string Json)> Put<T>(string ruta, T cuerpo, CancellationToken ct)
    {
        using var r = await http.PutAsJsonAsync(ruta, cuerpo, ct);
        return ((int)r.StatusCode, await r.Content.ReadAsStringAsync(ct));
    }
}

/// <summary>Inicio de sesión contra api/auth/ingresar. Devuelve la sesión o el mensaje de error de la API.</summary>
public sealed class ProBankAuthApi(HttpClient http)
{
    public async Task<(IngresoResponse? Sesion, string? Error)> Ingresar(IngresarRequest r, CancellationToken ct)
    {
        using var resp = await http.PostAsJsonAsync("api/auth/ingresar", r, ct);
        if (resp.IsSuccessStatusCode) return (await resp.Content.ReadFromJsonAsync<IngresoResponse>(ct), null);
        var problema = await resp.Content.ReadFromJsonAsync<Problema>(ct);
        return (null, problema?.Detail ?? "No se pudo iniciar sesión.");
    }

    private sealed record Problema(string? Title, string? Detail);
}
