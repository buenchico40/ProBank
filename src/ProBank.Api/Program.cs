using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ProBank.Api.BackgroundJobs;
using ProBank.Api.Infrastructure;
using ProBank.Api.Seguridad;
using ProBank.Application;
using ProBank.Application.Abstractions;
using ProBank.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Identidad (SEG-02/03): usuario y contraseña en seg.Usuario; la API emite y valida su propio JWT firmado.
var jwt = builder.Configuration.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<EmisorTokens>();
builder.Services.AddScoped<ServicioIngreso>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // conserva "name", "roles", "probank_uid" e "ipaddr" tal cual
        o.TokenValidationParameters = jwt.Validacion();
    });
builder.Services.AddAuthorization();

// Límite de peticiones por usuario
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErroresSql>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ISesiones, SesionesCache>();
builder.Services.AddOpenApi();
// App Insights 3.x falla al arrancar sin cadena de conexión; en local y en pruebas se omite.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? builder.Configuration["ApplicationInsights:ConnectionString"]))
    builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
// Los textos (códigos, mensajes) también se responden como JSON, igual que el resto de la API.
builder.Services.AddControllers(o => o.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.StringOutputFormatter>());

builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ProgramadorConsultas>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<IpSesionMiddleware>();
app.UseMiddleware<SesionVigenteMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers().RequireAuthorization();
app.Run();

public partial class Program { }
