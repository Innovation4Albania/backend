using Npgsql;

namespace Innovation4Albania.DashboardBackend.Api.Data;

public sealed class PostgresDashboardStorePersistence : IDashboardStorePersistence
{
    private const string StateId = "default";

    private readonly string? _connectionString;
    private readonly ILogger<PostgresDashboardStorePersistence> _logger;

    public PostgresDashboardStorePersistence(IConfiguration configuration, ILogger<PostgresDashboardStorePersistence> logger)
    {
        _logger = logger;
        _connectionString = ResolveConnectionString(configuration);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<string?> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = new NpgsqlCommand(
            "select payload::text from dashboard_state where id = @id",
            connection);
        command.Parameters.AddWithValue("id", StateId);

        var payload = await command.ExecuteScalarAsync(cancellationToken);
        return payload as string;
    }

    public async Task SaveSnapshotAsync(string payload, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            insert into dashboard_state (id, payload, updated_at)
            values (@id, @payload::jsonb, now())
            on conflict (id)
            do update set payload = excluded.payload, updated_at = now()
            """,
            connection);
        command.Parameters.AddWithValue("id", StateId);
        command.Parameters.AddWithValue("payload", payload);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureSchemaAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            create table if not exists dashboard_state (
                id text primary key,
                payload jsonb not null,
                updated_at timestamptz not null default now()
            )
            """,
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"]
            ?? configuration["POSTGRES_URL"]
            ?? configuration["POSTGRES_INTERNAL_URL"];

        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        return configured.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || configured.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            ? ConvertPostgresUrl(configured)
            : configured;
    }

    private static string ConvertPostgresUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
            Password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty),
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
