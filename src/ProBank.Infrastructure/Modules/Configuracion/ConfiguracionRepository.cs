using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Configuracion;
using ProBank.Contracts.Configuracion;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Modules.Configuracion;

/// <summary>Configuración global (cfg.Parametro). Solo procedimientos del esquema cfg.</summary>
internal sealed class ConfiguracionRepository(ISqlConnectionFactory db) : IConfiguracionRepository
{
    public async Task<IReadOnlyList<ParametroGlobalDto>> Listar(CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        return (await cn.QueryAsync<ParametroGlobalDto>(new CommandDefinition("cfg.usp_Parametro_Listar", commandType: Sp.Tipo, cancellationToken: ct))).ToList();
    }

    public async Task<int> Guardar(IReadOnlyDictionary<string, string?> valores, Actor a, CancellationToken ct)
    {
        var p = new DynamicParameters(new
        {
            ValoresJson = JsonSerializer.Serialize(valores.Select(kv => new { clave = kv.Key, valor = kv.Value })),
            ActorId = a.UsuarioId, a.RolActivo, a.Ip,
        });
        p.Add("Cambios", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("cfg.usp_Parametro_Guardar", p, commandType: Sp.Tipo, cancellationToken: ct));
        return p.Get<int>("Cambios");
    }
}

/// <summary>
/// Valores vigentes con caché de un minuto (la API que guarda la invalida al instante; otras instancias lo notan al vencer).
/// Sin valor en la base, toma el de la configuración del servidor (Correo.Smtp.Servidor → Correo:Smtp:Servidor).
/// </summary>
internal sealed class ConfiguracionGlobal(ISqlConnectionFactory db, IConfiguration config) : IConfiguracionGlobal
{
    // Referencia inmutable: se reemplaza entera, sin lecturas a medio escribir entre hilos
    private sealed record Cache(IReadOnlyDictionary<string, string?> Valores, DateTime Vence);
    private volatile Cache? _cache;

    public bool PermiteCarpeta => !string.IsNullOrWhiteSpace(config["Correo:CarpetaLocal"]);

    public async Task<IReadOnlyDictionary<string, string?>> Valores(CancellationToken ct)
    {
        if (_cache is { } c && c.Vence > DateTime.UtcNow) return c.Valores;
        await using var cn = await db.Abrir(ct);
        var filas = await cn.QueryAsync<Fila>(new CommandDefinition("cfg.usp_Parametro_Listar", commandType: Sp.Tipo, cancellationToken: ct));
        var valores = filas.ToDictionary(f => f.Clave, f => string.IsNullOrWhiteSpace(f.Valor) ? config[f.Clave.Replace('.', ':')] : f.Valor);
        _cache = new Cache(valores, DateTime.UtcNow.AddMinutes(1));
        return valores;
    }

    public void Invalidar() => _cache = null;

    // Del listado solo interesan clave y valor (Dapper asigna por nombre de columna)
    private sealed class Fila
    {
        public string Clave { get; init; } = "";
        public string? Valor { get; init; }
    }
}
