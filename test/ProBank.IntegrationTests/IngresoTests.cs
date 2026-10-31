using Microsoft.IdentityModel.JsonWebTokens;
using ProBank.Api.Seguridad;
using ProBank.Application.Abstractions;
using ProBank.Contracts.Seguridad;
using ProBank.Domain.Common;

namespace ProBank.IntegrationTests;

public sealed class IngresoTests
{
    private const string Clave = "Clave-Segura-2026!";
    private static readonly (byte[] Hash, byte[] Sal) Guardada = Claves.Generar(Clave);
    private static readonly OpcionesJwt Jwt = new() { Clave = "clave-de-prueba-de-al-menos-32-bytes!!" };

    private sealed class CredencialesFalsas(CredencialUsuario? u) : ICredencialesRepository
    {
        public List<(bool Exitoso, string? Motivo)> Intentos { get; } = [];
        public Task<CredencialUsuario?> Obtener(string usuario, CancellationToken ct) => Task.FromResult(u?.Usuario == usuario ? u : null);
        public Task RegistrarIngreso(string usuario, bool exitoso, string? motivo, string ip, CancellationToken ct)
        {
            Intentos.Add((exitoso, motivo));
            return Task.CompletedTask;
        }
    }

    private static CredencialUsuario Ana(string estado = "Activo", bool bloqueado = false, string? ip = null) =>
        new(7, "ana.perez", "Ana Pérez", Guardada.Hash, Guardada.Sal, ip, estado, bloqueado, ["Gerencia de TI", "Consulta"]);

    private static (ServicioIngreso Servicio, CredencialesFalsas Repo) Crear(CredencialUsuario? u)
    {
        var repo = new CredencialesFalsas(u);
        return (new ServicioIngreso(repo, new EmisorTokens(Jwt, TimeProvider.System)), repo);
    }

    [Fact(DisplayName = "SEG-03 · clave correcta: token válido con usuario, roles e IP")]
    public async Task Correcta()
    {
        var (s, repo) = Crear(Ana());
        var r = await s.Ingresar(new IngresarRequest("ana.perez", Clave, "10.0.0.5"), default);

        Assert.True(r.IsSuccess);
        Assert.Equal(["Gerencia de TI", "Consulta"], r.Value.Roles);
        var v = await new JsonWebTokenHandler().ValidateTokenAsync(r.Value.Token, Jwt.Validacion());
        Assert.True(v.IsValid);
        Assert.Equal("7", v.Claims["probank_uid"]);
        Assert.Equal("10.0.0.5", v.Claims["ipaddr"]);
        Assert.Equal([(true, (string?)null)], repo.Intentos);
    }

    [Fact(DisplayName = "SEG-03 · clave incorrecta y usuario inexistente dan el mismo mensaje")]
    public async Task MismoMensaje()
    {
        var (s, repo) = Crear(Ana());
        var mala = await s.Ingresar(new IngresarRequest("ana.perez", "otra-clave-cualquiera", "10.0.0.5"), default);
        var nadie = await s.Ingresar(new IngresarRequest("nadie", Clave, "10.0.0.5"), default);

        Assert.Equal(ServicioIngreso.Incorrecto, mala.Error);
        Assert.Equal(ServicioIngreso.Incorrecto, nadie.Error);
        Assert.All(repo.Intentos, i => Assert.False(i.Exitoso));
    }

    [Fact(DisplayName = "SEG-03 · cuenta bloqueada no entra aunque la clave sea correcta")]
    public async Task Bloqueada()
    {
        var (s, _) = Crear(Ana(bloqueado: true));
        Assert.Equal(ServicioIngreso.Bloqueado, (await s.Ingresar(new IngresarRequest("ana.perez", Clave, "10.0.0.5"), default)).Error);
    }

    [Fact(DisplayName = "Usuario inactivo no entra")]
    public async Task Inactivo()
    {
        var (s, _) = Crear(Ana(estado: "Inactivo"));
        Assert.True((await s.Ingresar(new IngresarRequest("ana.perez", Clave, "10.0.0.5"), default)).IsFailure);
    }

    [Fact(DisplayName = "Cuenta restringida a una IP solo entra desde esa IP")]
    public async Task IpPermitida()
    {
        var (s, _) = Crear(Ana(ip: "10.20.30.45"));
        Assert.Equal("forbidden", (await s.Ingresar(new IngresarRequest("ana.perez", Clave, "10.0.0.5"), default)).Error.Code);
        Assert.True((await s.Ingresar(new IngresarRequest("ana.perez", Clave, "10.20.30.45"), default)).IsSuccess);
    }

    [Fact(DisplayName = "Un token firmado con otra clave no es válido")]
    public async Task OtraClave()
    {
        var (s, _) = Crear(Ana());
        var r = await s.Ingresar(new IngresarRequest("ana.perez", Clave, "10.0.0.5"), default);
        var otra = new OpcionesJwt { Clave = "otra-clave-distinta-de-al-menos-32-bytes" };
        Assert.False((await new JsonWebTokenHandler().ValidateTokenAsync(r.Value.Token, otra.Validacion())).IsValid);
    }
}
