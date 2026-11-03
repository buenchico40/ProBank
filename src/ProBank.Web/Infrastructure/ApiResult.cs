using Microsoft.AspNetCore.Mvc;
using ProBank.Web.ApiClients;

namespace ProBank.Web.Infrastructure;

public static class ApiResult
{
    public static ContentResult Json((int Status, string Json) r) => new() { StatusCode = r.Status, Content = r.Json, ContentType = "application/json" };
}

/// <summary>Pasa al navegador el archivo que devuelve la API (o su error en JSON), en línea o como descarga.</summary>
public static class ArchivoApi
{
    public static async Task<IActionResult> Pasar(Controller c, ProBankApi api, string ruta, CancellationToken ct, bool descargar = false)
    {
        var r = await api.GetRespuesta(ruta, ct);
        if (!r.IsSuccessStatusCode)
        {
            using (r) return ApiResult.Json(((int)r.StatusCode, await r.Content.ReadAsStringAsync(ct)));
        }
        c.HttpContext.Response.RegisterForDispose(r);
        var tipo = r.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var cuerpo = await r.Content.ReadAsStreamAsync(ct);
        var nombre = r.Content.Headers.ContentDisposition?.FileNameStar ?? r.Content.Headers.ContentDisposition?.FileName?.Trim('"');
        if (descargar) return c.File(cuerpo, tipo, nombre ?? "documento");
        if (nombre is not null) c.Response.Headers.ContentDisposition = new System.Net.Mime.ContentDisposition { Inline = true, FileName = nombre }.ToString();
        return c.File(cuerpo, tipo);
    }
}
