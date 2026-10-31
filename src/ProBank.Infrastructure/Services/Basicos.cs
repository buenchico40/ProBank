using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Services;

internal sealed class EcuadorClock : IClock
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
    public DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);
    public DateOnly Hoy => DateOnly.FromDateTime(Ahora);
}

/// <summary>Auditoría general (aud.Auditoria): no editable ni eliminable; el SP solo inserta.</summary>
internal sealed class SqlAuditLog(ISqlConnectionFactory db, ICurrentUser user) : IAuditLog
{
    public async Task Registrar(string modulo, string accion, string detalle, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("aud.usp_Auditoria_Registrar",
            new { UsuarioId = user.UsuarioId, RolActivo = user.RolActivo, Modulo = modulo, Accion = accion, Detalle = detalle, Ip = user.Ip },
            commandType: Sp.Tipo, cancellationToken: ct));
    }
}
