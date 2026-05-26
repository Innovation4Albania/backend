using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Services;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Innovation4Albania.DashboardBackend.Api.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddOptions<GeminiOptions>()
            .BindConfiguration("Gemini");

        services.AddHttpClient(Options.DefaultName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddSingleton<IDashboardStorePersistence, PostgresDashboardStorePersistence>();
        services.AddSingleton<InnovationDashboardStore>();
        services.AddHostedService<DashboardStoreInitializer>();
        services.AddSingleton<IInnovationDashboardRepository, InnovationDashboardRepository>();
        services.AddSingleton<IUserRepository, PostgresUserRepository>();

        services.AddSingleton<IUserContextService, UserContextService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IReferenceDataService, ReferenceDataService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IPortfolioService, PortfolioService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<ICalendarService, CalendarService>();
        services.AddSingleton<IAiService, AiService>();

        return services;
    }
}

