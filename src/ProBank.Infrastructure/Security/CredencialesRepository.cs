using Dapper;
using ProBank.Application.Abstractions;
using ProBank.Infrastructure.Persistence;

namespace ProBank.Infrastructure.Security;

internal sealed class CredencialesRepository(ISqlConnectionFactory db) : ICredencialesRepository
{
    public async Task<CredencialUsuario?> Obtener(string usuario, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        using var m = await cn.QueryMultipleAsync(new CommandDefinition("seg.usp_Usuario_ObtenerCredencial",
            new { Usuario = usuario }, commandType: Sp.Tipo, cancellationToken: ct));
        var f = await m.ReadSingleOrDefaultAsync<Fila>();
        var roles = (await m.ReadAsync<string>()).ToList();
        return f is null ? null
            : new CredencialUsuario(f.UsuarioId, f.Usuario, f.Nombre, f.ClaveHash, f.ClaveSal, f.IpPermitida, f.Estado, f.Bloqueado, roles,
                f.RolPredeterminado, f.DebeCambiarClave, f.SelloSesion, f.MinutosSesion);
    }

    public async Task RegistrarIngreso(string usuario, bool exitoso, string? motivo, string ip, CancellationToken ct)
    {
        await using var cn = await db.Abrir(ct);
        await cn.ExecuteAsync(new CommandDefinition("seg.usp_Usuario_RegistrarIngreso",
            new { Usuario = usuario, Exitoso = exitoso, Motivo = motivo, Ip = ip }, commandType: Sp.Tipo, cancellationToken: ct));
    }

    private sealed record Fila(int UsuarioId, string Usuario, string Nombre, byte[]? ClaveHash, byte[]? ClaveSal,
        string? IpPermitida, string Estado, bool Bloqueado, string? RolPredeterminado, bool DebeCambiarClave, int SelloSesion, int MinutosSesion);
}
