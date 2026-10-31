using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProBank.Api.Seguridad;
using ProBank.Application.Abstractions;

namespace ProBank.IntegrationTests;

/// <summary>SEG-07 sin base de datos: el sello de la sesión se reemplaza por uno fijo.</summary>
[Trait("Category", "Integration")]
public sealed class SesionTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ClaveJwt = "clave-de-prueba-de-al-menos-32-bytes!!";

    private sealed class SelloFijo(int sello, string estado = "Activo") : ISesiones
    {
        public Task<SesionVigente?> Obtener(int usuarioId, CancellationToken ct) => Task.FromResult<SesionVigente?>(new SesionVigente(sello, estado));
        public void Invalidar(int usuarioId) { }
    }

    private HttpClient Cliente(ISesiones sesiones) => factory.WithWebHostBuilder(b => b
            .UseSetting("ConnectionStrings:ProBank", "Server=.;Database=x;Trusted_Connection=True")
            .UseSetting("Jwt:Clave", ClaveJwt)
            .ConfigureTestServices(s => s.AddSingleton(sesiones)))
        .CreateClient();

    private static string Token(int sello, bool cambiarClave = false) =>
        new EmisorTokens(new OpcionesJwt { Clave = ClaveJwt }, TimeProvider.System)
            .Emitir(new CredencialUsuario(7, "ana.perez", "Ana Pérez", null, null, null, "Activo", false, ["Consulta"],
                SelloSesion: sello, DebeCambiarClave: cambiarClave), "127.0.0.1").Token;

    private static async Task<(HttpStatusCode Status, string? Codigo)> Llamar(HttpClient c, string token, string ruta)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, ruta);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-Forwarded-For", "127.0.0.1");
        using var r = await c.SendAsync(req);
        var cuerpo = await r.Content.ReadAsStringAsync();
        return (r.StatusCode, cuerpo.StartsWith('{') ? JsonDocument.Parse(cuerpo).RootElement.GetProperty("title").GetString() : null);
    }

    [Fact(DisplayName = "SEG-07 · un token con sello anterior (sesión revocada) se rechaza")]
    public async Task SelloAnterior()
    {
        var (status, codigo) = await Llamar(Cliente(new SelloFijo(2)), Token(sello: 1), "/api/portafolio/tareas");
        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Equal("sesion_revocada", codigo);
    }

    [Fact(DisplayName = "SEG-07 · un usuario inactivo pierde sus sesiones")]
    public async Task Inactivo()
    {
        var (status, _) = await Llamar(Cliente(new SelloFijo(1, "Inactivo")), Token(sello: 1), "/api/portafolio/tareas");
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact(DisplayName = "SEG-07 · con clave temporal solo se permite el perfil")]
    public async Task ClaveTemporal()
    {
        var (status, codigo) = await Llamar(Cliente(new SelloFijo(1)), Token(sello: 1, cambiarClave: true), "/api/portafolio/tareas");
        Assert.Equal(HttpStatusCode.Forbidden, status);
        Assert.Equal("cambiar_clave", codigo);
    }
}
