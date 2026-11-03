using System.Collections.Concurrent;
using Azure.Security.KeyVault.Secrets;

namespace ProBank.Infrastructure.Security;

public interface ISecretProvider
{
    Task<string> Obtener(string nombre, CancellationToken ct);
}

/// <summary>Lee secretos de Key Vault con una caché corta (5 minutos) para tomar rotaciones sin reiniciar.</summary>
internal sealed class KeyVaultSecretProvider(SecretClient client) : ISecretProvider
{
    private readonly ConcurrentDictionary<string, (string Valor, DateTime Vence)> _cache = new();

    public async Task<string> Obtener(string nombre, CancellationToken ct)
    {
        if (_cache.TryGetValue(nombre, out var c) && c.Vence > DateTime.UtcNow) return c.Valor;
        var s = await client.GetSecretAsync(nombre, cancellationToken: ct);
        _cache[nombre] = (s.Value.Value, DateTime.UtcNow.AddMinutes(5));
        return s.Value.Value;
    }
}

/// <summary>
/// Sin KeyVault:Uri configurado (desarrollo local sin Azure): lee el secreto de la configuración (Secretos:&lt;nombre&gt;, en
/// user-secrets, nunca en appsettings); si no está, falla indicando qué configurar.
/// </summary>
internal sealed class SinKeyVault(Microsoft.Extensions.Configuration.IConfiguration config) : ISecretProvider
{
    public Task<string> Obtener(string nombre, CancellationToken ct) =>
        config["Secretos:" + nombre] is { Length: > 0 } valor ? Task.FromResult(valor)
            : throw new InvalidOperationException($"El secreto {nombre} no está disponible: configure KeyVault:Uri (o, en desarrollo, Secretos:{nombre} en user-secrets).");
}
