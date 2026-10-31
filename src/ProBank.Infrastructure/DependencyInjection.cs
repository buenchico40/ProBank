using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using ProBank.Application.Abstractions;
using ProBank.Application.Modules.Catalogos;
using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Application.Modules.Coactivas;
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
            : new SinKeyVault());

        services.AddSingleton(new GraphServiceClient(credencial, ["https://graph.microsoft.com/.default"]));
        services.AddScoped<IMailSender, GraphMailSender>();

        // Módulos
        services.AddScoped<ICatalogoRepository, CatalogoRepository>();
        services.AddScoped<ISeguridadRepository, SeguridadRepository>();
        services.AddScoped<IPanelRepository, PanelRepository>();
        services.AddScoped<ICoactivasRepository, CoactivasRepository>();
        services.AddScoped<ITareaRepository, TareaRepository>();
        services.AddScoped<IPortafolioRepository, PortafolioRepository>();
        services.AddScoped<IConsultaRepository, ConsultaRepository>();
        services.AddScoped<IConsultaExecutor, SqlConsultaExecutor>();
        services.AddScoped<IProgramacionRepository, ProgramacionRepository>();
        services.AddSingleton<IExportador, ExportadorCsv>();
        services.AddSingleton<IExportador, ExportadorTexto>();
        services.AddSingleton<IExportador, ExportadorJson>();
        return services;
    }
}
