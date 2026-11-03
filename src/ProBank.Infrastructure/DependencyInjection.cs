using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Catalogos;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Application.Modules.Coactivas;
using ProBank.Application.Modules.Configuracion;
using ProBank.Infrastructure.Modules.Configuracion;
using ProBank.Application.Modules.Panel;
using ProBank.Infrastructure.Modules.Coactivas;
using ProBank.Application.Modules.Portafolio;
using ProBank.Application.Modules.Seguridad;
using ProBank.Infrastructure.Modules.Panel;
using ProBank.Infrastructure.Integrations;
using ProBank.Infrastructure.Modules.Catalogos;
using ProBank.Infrastructure.Modules.ConsultasDinamicas;
using ProBank.Infrastructure.Modules.Portafolio;
using ProBank.Infrastructure.Modules.Seguridad;
using ProBank.Infrastructure.Persistence;
using ProBank.Infrastructure.Security;
using ProBank.Infrastructure.Services;

namespace ProBank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Identidad administrada en Azure; en local usa Azure CLI / Visual Studio Code.
        var credencial = new DefaultAzureCredential();
        AlwaysEncrypted.Registrar(credencial);

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<IClock, EcuadorClock>();
        services.AddScoped<IAuditLog, SqlAuditLog>();
        services.AddScoped<ICredencialesRepository, CredencialesRepository>();
        services.AddScoped<IPermisos, SqlPermisos>();

        var kv = config["KeyVault:Uri"];
        if (!string.IsNullOrWhiteSpace(kv))
            services.AddSingleton(new SecretClient(new Uri(kv), credencial));
        services.AddSingleton<ISecretProvider>(sp => sp.GetService<SecretClient>() is { } c
            ? new KeyVaultSecretProvider(c)
            : new SinKeyVault(config));

        // Documentos adjuntos: Blob Storage si hay cuenta configurada; si no (local), una carpeta del equipo
        var blob = config["Almacen:BlobEndpoint"];
        services.AddSingleton<IAlmacenArchivos>(string.IsNullOrWhiteSpace(blob)
            ? new CarpetaAlmacen(config["Almacen:CarpetaLocal"] is { Length: > 0 } carpeta ? carpeta
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProBank", "archivos"))
            : new BlobAlmacen(new Azure.Storage.Blobs.BlobServiceClient(new Uri(blob), credencial)));

        // CFG-01: configuración global (Configuración › Parámetros generales) con caché corta
        services.AddSingleton<IConfiguracionGlobal, ConfiguracionGlobal>();
        services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();

        // SQL-08 y CFG-03: el proveedor de correo (Graph, Smtp o Carpeta, esta solo en desarrollo) se elige en cada envío según la
        // configuración global, así un cambio se aplica sin reiniciar (ADR 0011)
        services.AddSingleton(new GraphServiceClient(credencial, ["https://graph.microsoft.com/.default"]));
        var carpetaCorreo = config["Correo:CarpetaLocal"] is { Length: > 0 } c ? c
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProBank", "correos");
        services.AddScoped<IMailSender>(sp => new CorreoSegunConfiguracion(sp.GetRequiredService<IConfiguracionGlobal>(),
            sp.GetRequiredService<GraphServiceClient>(), sp.GetRequiredService<ISecretProvider>(), carpetaCorreo));

        // Módulos
        services.AddScoped<ICatalogoRepository, CatalogoRepository>();
        services.AddScoped<ISeguridadRepository, SeguridadRepository>();
        services.AddScoped<IPanelRepository, PanelRepository>();
        services.AddScoped<ICoactivasRepository, CoactivasRepository>();
        services.AddScoped<ITareaRepository, TareaRepository>();
        services.AddScoped<IPortafolioRepository, PortafolioRepository>();
        services.AddSingleton<IGeneradorInforme, Reportes.GeneradorInformeDevExpress>();
        services.AddSingleton<Application.Modules.Presupuesto.IGeneradorReportePresupuesto, Reportes.GeneradorReportePresupuestoDevExpress>();
        services.AddScoped<Application.Modules.Marketing.IMarketingRepository, Modules.Marketing.MarketingRepository>();
        services.AddScoped<Application.Modules.Presupuesto.IPresupuestoRepository, Modules.Presupuesto.PresupuestoRepository>();
        services.AddScoped<Application.Modules.Boveda.IBovedaRepository, Modules.Boveda.BovedaRepository>();
        services.AddScoped<IConsultaRepository, ConsultaRepository>();
        services.AddScoped<IConsultaExecutor, SqlConsultaExecutor>();
        services.AddScoped<IProgramacionRepository, ProgramacionRepository>();
        services.AddSingleton<IExportador, ExportadorCsv>();
        services.AddSingleton<IExportador, ExportadorTexto>();
        services.AddSingleton<IExportador, ExportadorJson>();
        services.AddSingleton<IExportador, Reportes.ExportadorXlsx>();
        services.AddSingleton<IExportador, Reportes.ExportadorPdf>();
        return services;
    }
}
