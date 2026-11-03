using System.Net;
using ProBank.Application.Abstractions;
using ProBank.Contracts.Configuracion;
using ProBank.Domain.Common;
using ProBank.Domain.Modules.Configuracion;

namespace ProBank.Application.Modules.Configuracion;

public interface IConfiguracionRepository
{
    Task<IReadOnlyList<ParametroGlobalDto>> Listar(CancellationToken ct);
    /// <summary>Devuelve cuántos parámetros cambiaron.</summary>
    Task<int> Guardar(IReadOnlyDictionary<string, string?> valores, Actor actor, CancellationToken ct);
}

/// <summary>
/// Valores vigentes de la configuración global, con caché corta. Un parámetro sin valor en la base toma el de la configuración
/// del servidor (Correo.Smtp.Servidor → Correo:Smtp:Servidor), útil antes de configurarlo por primera vez.
/// </summary>
public interface IConfiguracionGlobal
{
    Task<IReadOnlyDictionary<string, string?>> Valores(CancellationToken ct);
    void Invalidar();
    /// <summary>Solo en desarrollo (hay Correo:CarpetaLocal en la configuración del servidor) se acepta el proveedor Carpeta.</summary>
    bool PermiteCarpeta { get; }
}

public sealed record ListarParametrosGlobalesQuery : IQuery<IReadOnlyList<ParametroGlobalDto>>;
public sealed record GuardarParametrosGlobalesCommand(GuardarParametrosGlobalesRequest Request) : ICommand<string>;
/// <summary>CFG-03: envía un correo de prueba con la configuración guardada.</summary>
public sealed record ProbarCorreoCommand(string Destinatario) : ICommand<string>;

internal sealed class ConfiguracionHandlers(IConfiguracionRepository repo, IConfiguracionGlobal global, IPermisos permisos, ICurrentUser user,
    IMailSender correo, IAuditLog audit, IClock clock) :
    IQueryHandler<ListarParametrosGlobalesQuery, IReadOnlyList<ParametroGlobalDto>>,
    ICommandHandler<GuardarParametrosGlobalesCommand, string>,
    ICommandHandler<ProbarCorreoCommand, string>
{
    public async Task<Result<IReadOnlyList<ParametroGlobalDto>>> Handle(ListarParametrosGlobalesQuery q, CancellationToken ct) =>
        await permisos.Exigir("config", "ver", ct) is { } e ? Result.Failure<IReadOnlyList<ParametroGlobalDto>>(e) : Result.Success(await repo.Listar(ct));

    public async Task<Result<string>> Handle(GuardarParametrosGlobalesCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("config", "editar", ct) is { } e) return Result.Failure<string>(e);
        var actuales = await repo.Listar(ct);
        var cambios = (c.Request.Valores ?? new Dictionary<string, string?>()).ToDictionary(kv => kv.Key, kv => string.IsNullOrWhiteSpace(kv.Value) ? null : kv.Value.Trim());
        if (cambios.Keys.FirstOrDefault(k => actuales.All(p => p.Clave != k)) is { } desconocida)
            return Result.Failure<string>(Error.Validation("clave", $"El parámetro {desconocida} no existe."));

        // Se valida el conjunto completo con los cambios aplicados (las reglas del correo cruzan varios parámetros)
        var valores = actuales.ToDictionary(p => p.Clave, p => cambios.TryGetValue(p.Clave, out var v) ? v : p.Valor);
        var definicion = actuales.Select(p => new ParametroGlobal(p.Clave, p.Categoria, p.Nombre, p.Tipo, p.Opciones, p.Minimo, p.Maximo, p.Prefijo, valores[p.Clave])).ToList();
        if (ReglasConfiguracion.Validar(definicion, valores, global.PermiteCarpeta) is { } v) return Result.Failure<string>(v);

        var n = await repo.Guardar(cambios, Actor.De(user), ct);
        global.Invalidar();
        return Result.Success(n == 0 ? "No hubo cambios" : $"{n} parámetro(s) guardados: se aplican de inmediato");
    }

    public async Task<Result<string>> Handle(ProbarCorreoCommand c, CancellationToken ct)
    {
        if (await permisos.Exigir("config", "editar", ct) is { } e) return Result.Failure<string>(e);
        var destino = c.Destinatario?.Trim() ?? "";
        if (!destino.Contains('@') || destino.Length > 256) return Result.Failure<string>(Error.Validation("destinatario", "Indique un correo válido."));
        var proveedor = ReglasConfiguracion.Valor(await global.Valores(ct), ReglasConfiguracion.Proveedor) ?? "Graph";
        try
        {
            await correo.Enviar([destino], "ProBank · prueba de correo",
                $"<p>Correo de prueba de ProBank enviado por <b>{WebUtility.HtmlEncode(proveedor)}</b> el {clock.Ahora:dd/MM/yyyy HH:mm} " +
                $"a pedido de {WebUtility.HtmlEncode(user.Nombre)}.</p><p>Si lo recibe, la configuración de correo funciona.</p>", null, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var motivo = ex.Message.Length > 300 ? ex.Message[..300] : ex.Message;
            await audit.Registrar("Configuración", "Prueba de correo fallida", $"{proveedor} a {destino}: {motivo}", ct);
            return Result.Failure<string>(new Error("correo", $"No se pudo enviar por {proveedor}: {motivo}"));
        }
        await audit.Registrar("Configuración", "Prueba de correo", $"{proveedor} a {destino}", ct);
        return Result.Success($"Correo de prueba enviado a {destino} por {proveedor}");
    }
}
