using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace ProBank.Infrastructure.Security;

/// <summary>
/// DEC-04: certificado del banco con el que se firman los PDF de Decisiones. Es un certificado de Key Vault (secreto
/// <c>firma-documentos</c>, o el de FirmaDocumentos:Secreto): Key Vault expone su clave privada como un secreto PKCS#12 en base64.
/// En la máquina local sin Key Vault, si no hay Secretos:firma-documentos en user-secrets, se usa uno autofirmado de desarrollo
/// (el PDF queda firmado, pero el certificado no es de confianza).
/// </summary>
public sealed class CertificadoFirma(ISecretProvider secretos, IConfiguration config)
{
    private readonly string _secreto = config["FirmaDocumentos:Secreto"] is { Length: > 0 } s ? s : "firma-documentos";
    private readonly bool _local = string.IsNullOrWhiteSpace(config["KeyVault:Uri"]);
    private static readonly Lazy<X509Certificate2> Desarrollo = new(CrearDesarrollo);

    public async Task<X509Certificate2> Obtener(CancellationToken ct)
    {
        string pfx;
        try { pfx = await secretos.Obtener(_secreto, ct); }
        catch (InvalidOperationException) when (_local) { return Desarrollo.Value; }
        return X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(pfx), null, X509KeyStorageFlags.EphemeralKeySet);
    }

    private static X509Certificate2 CrearDesarrollo()
    {
        using var rsa = RSA.Create(3072);
        var pedido = new CertificateRequest("CN=ProBank Firma de documentos (desarrollo), O=ProBank", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        pedido.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, critical: true));
        using var c = pedido.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2));
        return X509CertificateLoader.LoadPkcs12(c.Export(X509ContentType.Pkcs12), null, X509KeyStorageFlags.EphemeralKeySet);
    }
}
