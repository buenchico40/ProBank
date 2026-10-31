using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace ProBank.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "La API responde /health sin autenticación")]
    public async Task Health()
    {
        var client = factory.WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:ProBank", "Server=.;Database=x;Trusted_Connection=True")
                .UseSetting("Jwt:Clave", "clave-de-prueba-de-al-menos-32-bytes!!"))
            .CreateClient();
        var r = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact(DisplayName = "SEG-02 · los endpoints exigen el token de la sesión")]
    public async Task SinToken()
    {
        var client = factory.WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:ProBank", "Server=.;Database=x;Trusted_Connection=True")
                .UseSetting("Jwt:Clave", "clave-de-prueba-de-al-menos-32-bytes!!"))
            .CreateClient();
        var r = await client.GetAsync("/api/portafolio/tareas");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}

/// <summary>SQL Server en contenedor (Docker) para probar los procedimientos almacenados del .dacpac.</summary>
[Trait("Category", "Integration")]
public sealed class BaseDeDatosTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public Task InitializeAsync() => _sql.StartAsync();
    public Task DisposeAsync() => _sql.DisposeAsync().AsTask();

    [Fact(DisplayName = "El contenedor de SQL Server responde")]
    public async Task Conecta()
    {
        await using var cn = new SqlConnection(_sql.GetConnectionString());
        await cn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT 1", cn);
        Assert.Equal(1, (int)(await cmd.ExecuteScalarAsync())!);
        // Siguiente paso: publicar ProBank.Database.dacpac con Microsoft.SqlServer.DacFx y probar los SP de pry y rpt.
    }
}
