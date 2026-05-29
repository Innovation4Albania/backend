using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Data.Repositories;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class DatabaseSchemaInitializer(
    IDashboardStorePersistence dashboardPersistence,
    PostgresUserRepository userRepository,
    ILogger<DatabaseSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await dashboardPersistence.InitializeAsync(cancellationToken);
        await userRepository.InitializeAsync(cancellationToken);
        logger.LogInformation("Database schema initialization completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
