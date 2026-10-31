using Microsoft.AspNetCore.Authentication.Cookies;
using ProBank.Web.ApiClients;

var builder = WebApplication.CreateBuilder(args);

// Inicio de sesión con usuario y contraseña (SEG-03): la API valida la clave y emite el token; la Web lo guarda
// en su cookie de sesión y lo envía en cada llamada. La cookie vence junto con el token.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Cuenta/Ingresar";
        o.LogoutPath = "/Cuenta/Salir";
        o.AccessDeniedPath = "/Cuenta/Ingresar";
        o.Cookie.Name = "probank_sesion";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.SlidingExpiration = false;
    });

builder.Services.AddControllersWithViews(o => o.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter()));
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
// App Insights 3.x falla al arrancar sin cadena de conexión; en local y en pruebas se omite.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? builder.Configuration["ApplicationInsights:ConnectionString"]))
    builder.Services.AddApplicationInsightsTelemetry();

// Cliente tipado hacia la API: token de la sesión + rol activo + IP del usuario + resiliencia (Polly)
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiTokenHandler>();
builder.Services.AddHttpClient<ProBankApi>(c => c.BaseAddress = new Uri(builder.Configuration["ProBankApi:BaseUrl"]!))
    .AddHttpMessageHandler<ApiTokenHandler>()
    .AddStandardResilienceHandler();
// Cliente sin token solo para api/auth/ingresar; sin reintentos para no duplicar intentos fallidos.
builder.Services.AddHttpClient<ProBankAuthApi>(c => c.BaseAddress = new Uri(builder.Configuration["ProBankApi:BaseUrl"]!));

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("areas", "{area:exists}/{controller=Inicio}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
