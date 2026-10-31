using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProBank.Application.Abstractions;

namespace ProBank.Api.Seguridad;

/// <summary>
/// Configuración "Jwt": la clave de firma viene de Key Vault en Azure (referencia en App Service) y de user-secrets en local.
/// </summary>
public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";
    public string Clave { get; set; } = string.Empty;
    public string Emisor { get; set; } = "probank-api";
    public string Audiencia { get; set; } = "probank-web";
    public int MinutosSesion { get; set; } = 60;

    public SymmetricSecurityKey LlaveFirma()
    {
        var bytes = Encoding.UTF8.GetBytes(Clave);
        if (bytes.Length < 32) throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 bytes (Key Vault o user-secrets).");
        return new SymmetricSecurityKey(bytes);
    }

    public TokenValidationParameters Validacion() => new()
    {
        ValidIssuer = Emisor,
        ValidAudience = Audiencia,
        IssuerSigningKey = LlaveFirma(),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = "name",
        RoleClaimType = "roles",
        ClockSkew = TimeSpan.FromMinutes(1),
    };
}

/// <summary>Emite el token con los mismos claims que lee <see cref="Infrastructure.HttpCurrentUser"/>.</summary>
public sealed class EmisorTokens(OpcionesJwt opciones, TimeProvider reloj)
{
    public const string ClaimSello = "probank_sv";
    public const string ClaimCambiarClave = "cambiar_clave";

    public (string Token, DateTimeOffset Expira) Emitir(CredencialUsuario u, string ip)
    {
        // La duración se parametriza en Seguridad › Parámetros de acceso; sin ese dato, la de la configuración.
        var expira = reloj.GetUtcNow().AddMinutes(u.MinutosSesion > 0 ? u.MinutosSesion : opciones.MinutosSesion);
        var claims = new List<Claim>
        {
            new("sub", u.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new("probank_uid", u.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new("name", u.Nombre),
            new("preferred_username", u.Usuario),
            new("ipaddr", ip),
            new(ClaimSello, u.SelloSesion.ToString(CultureInfo.InvariantCulture)),
        };
        if (u.DebeCambiarClave) claims.Add(new Claim(ClaimCambiarClave, "1"));
        claims.AddRange(u.Roles.Select(r => new Claim("roles", r)));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = opciones.Emisor,
            Audience = opciones.Audiencia,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = reloj.GetUtcNow().UtcDateTime,
            NotBefore = reloj.GetUtcNow().UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = new SigningCredentials(opciones.LlaveFirma(), SecurityAlgorithms.HmacSha256),
        });
        return (token, expira);
    }
}
