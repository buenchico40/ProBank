using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ProBank.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> Abrir(CancellationToken ct);
}

/// <summary>Conexión a la base de ProBank. La cadena incluye "Column Encryption Setting=Enabled" (Always Encrypted).</summary>
internal sealed class SqlConnectionFactory(IConfiguration config) : ISqlConnectionFactory
{
    private readonly string _cs = config.GetConnectionString("ProBank")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:ProBank (Key Vault o user-secrets).");

    public async Task<SqlConnection> Abrir(CancellationToken ct)
    {
        var cn = new SqlConnection(_cs);
        await cn.OpenAsync(ct);
        return cn;
    }
}

internal static class Sp
{
    public const CommandType Tipo = CommandType.StoredProcedure;
}
