using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;

// CRE-03: llaves de Always Encrypted de la Bóveda. La llave maestra (CMK) vive fuera de la base: en Azure Key Vault o, en la máquina
// local, en un certificado del almacén del usuario. La llave de columna (CEK) se guarda en la base cifrada con la CMK.
// El .dacpac no despliega estas llaves (cada ambiente tiene las suyas): se crean aquí una vez, antes de publicar la base.
internal static class Llaves
{
    public const string Cmk = "CMK_ProBank";
    public const string Cek = "CEK_ProBank";
    private const string SujetoLocal = "CN=ProBank Always Encrypted (local)";

    /// <param name="llaveKeyVault">Identificador de la llave en Key Vault (salida cmk_id de Terraform); sin él, certificado local.</param>
    public static async Task<int> Crear(string cs, string? llaveKeyVault)
    {
        await CrearBaseSiFalta(cs);
        await using var cn = new SqlConnection(cs);
        var existe = await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sys.column_encryption_keys WHERE name = @Cek", new { Cek });
        if (existe > 0)
        {
            Console.WriteLine($"La base ya tiene {Cek}; no se cambia (para rotar la CMK use docs/runbooks/rotacion-llaves.md).");
            return 0;
        }

        string proveedor, ruta;
        SqlColumnEncryptionKeyStoreProvider cifrador;
        if (string.IsNullOrWhiteSpace(llaveKeyVault))
        {
            var cert = CertificadoLocal();
            proveedor = SqlColumnEncryptionCertificateStoreProvider.ProviderName;
            ruta = $"CurrentUser/My/{cert.Thumbprint}";
            cifrador = new SqlColumnEncryptionCertificateStoreProvider();
        }
        else
        {
            proveedor = SqlColumnEncryptionAzureKeyVaultProvider.ProviderName;
            ruta = llaveKeyVault.Trim();
            cifrador = new SqlColumnEncryptionAzureKeyVaultProvider(new DefaultAzureCredential());
        }

        var valor = cifrador.EncryptColumnEncryptionKey(ruta, "RSA_OAEP", RandomNumberGenerator.GetBytes(32));
        // Los nombres y la ruta no son datos del usuario; el valor va como literal binario porque CREATE no acepta parámetros.
        var sql = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.column_master_keys WHERE name = N'{Cmk}')
                CREATE COLUMN MASTER KEY [{Cmk}] WITH (KEY_STORE_PROVIDER_NAME = N'{proveedor}', KEY_PATH = N'{ruta.Replace("'", "''")}');
            CREATE COLUMN ENCRYPTION KEY [{Cek}] WITH VALUES (COLUMN_MASTER_KEY = [{Cmk}], ALGORITHM = 'RSA_OAEP', ENCRYPTED_VALUE = 0x{Convert.ToHexString(valor)});
            """;
        await cn.ExecuteAsync(sql);
        Console.WriteLine($"Llaves listas: {Cmk} ({proveedor} · {ruta}) y {Cek}.");
        return 0;
    }

    /// <summary>
    /// En una máquina nueva la base aún no existe y las llaves deben estar antes de publicar el .dacpac: se crea vacía con la
    /// intercalación del proyecto. En Azure la crea Terraform.
    /// </summary>
    private static async Task CrearBaseSiFalta(string cs)
    {
        var b = new SqlConnectionStringBuilder(cs);
        var nombre = b.InitialCatalog;
        if (string.IsNullOrEmpty(nombre) || b.DataSource.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase)) return;
        b.InitialCatalog = "master";
        await using var cn = new SqlConnection(b.ConnectionString);
        if (await cn.ExecuteScalarAsync<int?>("SELECT DB_ID(@nombre)", new { nombre }) is not null) return;
        await cn.ExecuteAsync($"CREATE DATABASE [{nombre.Replace("]", "]]")}] COLLATE Modern_Spanish_CI_AS");
        Console.WriteLine($"Base {nombre} creada.");
    }

    /// <summary>Certificado de la máquina local (se crea la primera vez). Solo sirve a este usuario de Windows: es para desarrollo.</summary>
    private static X509Certificate2 CertificadoLocal()
    {
        using var almacen = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        almacen.Open(OpenFlags.ReadWrite);
        var actual = almacen.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, SujetoLocal, validOnly: false)
            .OfType<X509Certificate2>().Where(c => c.HasPrivateKey).OrderByDescending(c => c.NotAfter).FirstOrDefault();
        if (actual is not null) return actual;

        using var rsa = RSA.Create(2048);
        var pedido = new CertificateRequest(SujetoLocal, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        pedido.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment, critical: false));
        pedido.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.8.2.2")], critical: false));
        using var temporal = pedido.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
        // Se reimporta para que la llave privada quede guardada en el perfil del usuario (la del certificado en memoria es efímera).
        var cert = X509CertificateLoader.LoadPkcs12(temporal.Export(X509ContentType.Pkcs12), null,
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet);
        almacen.Add(cert);
        Console.WriteLine($"Certificado local creado: {SujetoLocal} ({cert.Thumbprint}) en CurrentUser\\My.");
        return cert;
    }
}
