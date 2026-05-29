namespace Innovation4Albania.DashboardBackend.Api.Data;

public interface IDashboardStorePersistence
{
    bool IsConfigured { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<string?> LoadSnapshotAsync(CancellationToken cancellationToken = default);

    Task SaveSnapshotAsync(string payload, CancellationToken cancellationToken = default);
}
