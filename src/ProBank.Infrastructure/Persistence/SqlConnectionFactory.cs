using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ProBank.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> Abrir(CancellationToken ct);
    /// <summary>
    /// Conexión con Always Encrypted habilitado (Column Encryption Setting=Enabled), sea cual sea la cadena: el driver cifra los
    /// parámetros y descifra las columnas cifradas con la CMK (Key Vault en Azure, certificado en local). Solo la usa la Bóveda,
    /// para no sumar a las demás consultas la ida a sp_describe_parameter_encryption.
    /// </summary>
    Task<SqlConnection> AbrirCifrada(CancellationToken ct);
}

/// <summary>Conexión a la base de ProBank.</summary>
internal sealed class SqlConnectionFactory(IConfiguration config) : ISqlConnectionFactory
{
    private readonly string _cs = config.GetConnectionString("ProBank")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:ProBank (Key Vault o user-secrets).");

    private string? _cifrada;

    public Task<SqlConnection> Abrir(CancellationToken ct) => Conectar(_cs, ct);

    public Task<SqlConnection> AbrirCifrada(CancellationToken ct) =>
        Conectar(_cifrada ??= new SqlConnectionStringBuilder(_cs) { ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Enabled }.ConnectionString, ct);

    private static async Task<SqlConnection> Conectar(string cs, CancellationToken ct)
    {
        var cn = new SqlConnection(cs);
        await cn.OpenAsync(ct);
        return cn;
    }
}

internal static class Sp
{
    public const CommandType Tipo = CommandType.StoredProcedure;
}
