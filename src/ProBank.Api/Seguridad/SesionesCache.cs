using Microsoft.Extensions.Caching.Memory;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Seguridad;

namespace ProBank.Api.Seguridad;

/// <summary>
/// SEG-07: sello de sesión por usuario con caché de 30 segundos, para no consultar la base en cada petición.
/// Una revocación hecha en otra instancia de la API se aplica a más tardar al vencer la caché.
/// </summary>
internal sealed class SesionesCache(IMemoryCache cache, IServiceScopeFactory scopes) : ISesiones
{
    private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(30);
    private static string Clave(int usuarioId) => $"sesion:{usuarioId}";

    public async Task<SesionVigente?> Obtener(int usuarioId, CancellationToken ct)
    {
        if (cache.TryGetValue(Clave(usuarioId), out SesionVigente? s)) return s;
        await using var scope = scopes.CreateAsyncScope();
        s = await scope.ServiceProvider.GetRequiredService<ISeguridadRepository>().ObtenerSello(usuarioId, ct);
        cache.Set(Clave(usuarioId), s, Duracion);
        return s;
    }

    public void Invalidar(int usuarioId) => cache.Remove(Clave(usuarioId));
}
