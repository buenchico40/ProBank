using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;

namespace ProBank.Infrastructure.Persistence;

/// <summary>Registra el proveedor de Key Vault para las llaves maestras (CMK) de Always Encrypted.</summary>
public static class AlwaysEncrypted
{
    private static int _registrado;

    public static void Registrar(TokenCredential credencial)
    {
        if (Interlocked.Exchange(ref _registrado, 1) == 1) return;
        var proveedor = new SqlColumnEncryptionAzureKeyVaultProvider(credencial);
        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
        {
            [SqlColumnEncryptionAzureKeyVaultProvider.ProviderName] = proveedor
        });
    }
}
