using ProBank.Domain.Modules.Configuracion;
using static ProBank.Domain.Modules.Configuracion.ReglasConfiguracion;

namespace ProBank.UnitTests.Configuracion;

public sealed class ConfiguracionTests
{
    // Definición como la del despliegue (Scripts/Seed.PostDeployment.sql)
    private static readonly ParametroGlobal[] Definicion =
    [
        new(Proveedor, "Correo", "Proveedor de correo", "Lista", "Graph,Smtp,Carpeta", null, null, null, null),
        new(Buzon, "Correo", "Buzón de servicio", "Correo", null, null, null, null, null),
        new(SmtpServidor, "Correo", "Servidor SMTP", "Texto", null, null, null, null, null),
        new(SmtpPuerto, "Correo", "Puerto SMTP", "Numero", null, 1, 65535, null, null),
        new(SmtpSeguridad, "Correo", "Cifrado SMTP", "Lista", "StartTls,TlsDirecto,Ninguna", null, null, null, null),
        new(SmtpUsuario, "Correo", "Usuario SMTP", "Texto", null, null, null, null, null),
        new(SmtpSecreto, "Correo", "Secreto SMTP", "Secreto", null, null, null, "smtp-", null),
        new(SmtpRemitente, "Correo", "Remitente SMTP", "Correo", null, null, null, null, null),
        new(DiasConservacion, "Consultas dinámicas", "Días", "Numero", null, 0, 3650, null, null),
    ];

    private static Dictionary<string, string?> Smtp(params (string Clave, string? Valor)[] cambios)
    {
        var v = new Dictionary<string, string?>
        {
            [Proveedor] = "Smtp", [SmtpServidor] = "smtp.banco.local", [SmtpPuerto] = "587", [SmtpSeguridad] = "StartTls",
            [SmtpUsuario] = "probank", [SmtpSecreto] = "smtp-probank-clave", [SmtpRemitente] = "probank@banco.local", [DiasConservacion] = "90",
        };
        foreach (var (k, x) in cambios) v[k] = x;
        return v;
    }

    [Fact(DisplayName = "CFG-03 · Microsoft 365 necesita el buzón de servicio")]
    public void Graph()
    {
        Assert.Equal(Buzon, Validar(Definicion, new Dictionary<string, string?> { [Proveedor] = "Graph" }, false)!.Code);
        Assert.Null(Validar(Definicion, new Dictionary<string, string?> { [Proveedor] = "Graph", [Buzon] = "probank@banco.ec" }, false));
    }

    [Fact(DisplayName = "CFG-03 · SMTP: servidor, remitente, y con usuario el secreto de su clave y cifrado")]
    public void SmtpReglas()
    {
        Assert.Null(Validar(Definicion, Smtp(), false));
        Assert.Null(Validar(Definicion, Smtp((SmtpUsuario, null), (SmtpSecreto, null), (SmtpSeguridad, "Ninguna"), (SmtpPuerto, "25")), false)); // relay interno
        Assert.Equal(SmtpServidor, Validar(Definicion, Smtp((SmtpServidor, " ")), false)!.Code);
        Assert.Equal(SmtpRemitente, Validar(Definicion, Smtp((SmtpRemitente, null)), false)!.Code);
        Assert.Equal(SmtpSecreto, Validar(Definicion, Smtp((SmtpSecreto, null)), false)!.Code);
        Assert.Equal(SmtpSeguridad, Validar(Definicion, Smtp((SmtpSeguridad, "Ninguna")), false)!.Code);
    }

    [Theory(DisplayName = "CFG-02 · el secreto es solo un nombre de Key Vault con el prefijo de su uso")]
    [InlineData("smtp-probank-clave", true)]
    [InlineData("jwt-clave", false)]                // otro secreto de Key Vault: no se puede usar como clave SMTP
    [InlineData("cnx-core", false)]
    [InlineData("smtp-clave con espacios", false)]
    [InlineData("Password=123", false)]
    public void Secreto(string secreto, bool valido) =>
        Assert.Equal(valido, Validar(Definicion, Smtp((SmtpSecreto, secreto)), false) is null);

    [Fact(DisplayName = "CFG-01 · cada valor según su tipo: número en rango, opción de la lista y correo")]
    public void Tipos()
    {
        Assert.Equal(SmtpPuerto, Validar(Definicion, Smtp((SmtpPuerto, "70000")), false)!.Code);
        Assert.Equal(SmtpPuerto, Validar(Definicion, Smtp((SmtpPuerto, "abc")), false)!.Code);
        Assert.Equal(SmtpSeguridad, Validar(Definicion, Smtp((SmtpSeguridad, "Ssl")), false)!.Code);
        Assert.Equal(SmtpRemitente, Validar(Definicion, Smtp((SmtpRemitente, "probank")), false)!.Code);
        Assert.Equal(DiasConservacion, Validar(Definicion, Smtp((DiasConservacion, "-1")), false)!.Code);
    }

    [Fact(DisplayName = "CFG-03 · el proveedor Carpeta es solo para desarrollo")]
    public void Carpeta()
    {
        var v = new Dictionary<string, string?> { [Proveedor] = "Carpeta" };
        Assert.Equal(Proveedor, Validar(Definicion, v, permiteCarpeta: false)!.Code);
        Assert.Null(Validar(Definicion, v, permiteCarpeta: true));
    }
}
