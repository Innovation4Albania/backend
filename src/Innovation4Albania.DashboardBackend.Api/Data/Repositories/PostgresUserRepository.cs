using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Constants;
using Npgsql;

namespace Innovation4Albania.DashboardBackend.Api.Data.Repositories;

public sealed class PostgresUserRepository : IUserRepository
{
    private readonly string? _connectionString;
    private readonly string? _bootstrapDirectorUsername;
    private readonly string? _bootstrapDirectorPasswordHash;
    private readonly string? _bootstrapAdminUsername;
    private readonly string? _bootstrapAdminPasswordHash;

    public PostgresUserRepository(IConfiguration configuration)
    {
        _connectionString = ResolveConnectionString(configuration);
        _bootstrapDirectorUsername = configuration[$"Auth:Users:{ApplicationRoles.DrejtorAgjencie}:Username"];
        _bootstrapDirectorPasswordHash = configuration[$"Auth:Users:{ApplicationRoles.DrejtorAgjencie}:Password"];
        _bootstrapAdminUsername = configuration[$"Auth:Users:{ApplicationRoles.Admin}:Username"];
        _bootstrapAdminPasswordHash = configuration[$"Auth:Users:{ApplicationRoles.Admin}:Password"];
    }

    public async Task<StoredUser?> GetUserByUsername(string username, CancellationToken cancellationToken = default)
    {
        if (!TryGetUsername(username, out var normalizedUsername) || _connectionString is null)
        {
            return null;
        }

        await using var connection = await OpenConnection(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            select id, username, password_hash, role, ministry, full_name, created_at, is_active
            from users
            where lower(username) = lower(@username)
            """,
            connection);
        command.Parameters.AddWithValue("username", normalizedUsername);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadStoredUser(reader) : null;
    }

    public async Task<StoredUser?> GetUserById(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id) || _connectionString is null)
        {
            return null;
        }

        await using var connection = await OpenConnection(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            select id, username, password_hash, role, ministry, full_name, created_at, is_active
            from users
            where id = @id
            """,
            connection);
        command.Parameters.AddWithValue("id", id.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadStoredUser(reader) : null;
    }

    public async Task<IReadOnlyList<ManagedUserResponse>> GetUsers(CancellationToken cancellationToken = default)
    {
        if (_connectionString is null)
        {
            return [];
        }

        await using var connection = await OpenConnection(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            select id, username, role, ministry, full_name, created_at, is_active
            from users
            order by is_active desc, full_name, username
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var users = new List<ManagedUserResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(ReadManagedUser(reader));
        }

        return users;
    }

    public async Task<(bool IsSuccess, ManagedUserResponse? Response, string? Error)> CreateUser(
        CreateUserRequest request,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        if (_connectionString is null)
        {
            return (false, null, "Databaza e përdoruesve nuk është konfiguruar.");
        }

        await using var connection = await OpenConnection(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            insert into users (id, username, password_hash, role, ministry, full_name, created_at, is_active)
            values (@id, @username, @password_hash, @role, @ministry, @full_name, now(), true)
            returning id, username, role, ministry, full_name, created_at, is_active
            """,
            connection);
        command.Parameters.AddWithValue("id", $"usr-{Guid.NewGuid():N}");
        command.Parameters.AddWithValue("username", request.Username.Trim());
        command.Parameters.AddWithValue("password_hash", passwordHash);
        command.Parameters.AddWithValue("role", request.Role.Trim());
        command.Parameters.AddWithValue("ministry", (object?)NormalizeOptional(request.Ministry) ?? DBNull.Value);
        command.Parameters.AddWithValue("full_name", request.FullName.Trim());

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return (true, ReadManagedUser(reader), null);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return (false, null, "Ky username ekziston tashmë.");
        }
    }

    public Task<(bool IsSuccess, string? Error)> UpdatePassword(
        string id,
        string passwordHash,
        CancellationToken cancellationToken = default) =>
        ExecuteUpdate(
            "update users set password_hash = @password_hash where id = @id and is_active = true",
            id,
            cancellationToken,
            command => command.Parameters.AddWithValue("password_hash", passwordHash));

    public async Task<(bool IsSuccess, string? Error)> UpdateCredentials(
        string id,
        string username,
        string? passwordHash,
        CancellationToken cancellationToken = default)
    {
        if (_connectionString is null)
        {
            return (false, "Databaza e përdoruesve nuk është konfiguruar.");
        }

        await using var connection = await OpenConnection(cancellationToken);
        await using var command = new NpgsqlCommand(
            passwordHash is null
                ? "update users set username = @username where id = @id and is_active = true"
                : "update users set username = @username, password_hash = @password_hash where id = @id and is_active = true",
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("username", username.Trim());
        if (passwordHash is not null)
        {
            command.Parameters.AddWithValue("password_hash", passwordHash);
        }

        try
        {
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected == 1 ? (true, null) : (false, "Përdoruesi nuk u gjet ose është joaktiv.");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return (false, "Ky username ekziston tashmë.");
        }
    }

    public async Task<(bool IsSuccess, string? Error)> UpdateUser(
        string id,
        string fullName,
        string username,
        string role,
        string? ministry,
        string? passwordHash,
        CancellationToken cancellationToken = default)
    {
        if (_connectionString is null)
        {
            return (false, "Databaza e përdoruesve nuk është konfiguruar.");
        }

        await using var connection = await OpenConnection(cancellationToken);
        await using var command = new NpgsqlCommand(
            passwordHash is null
                ? "update users set full_name = @full_name, username = @username, role = @role, ministry = @ministry where id = @id and is_active = true"
                : "update users set full_name = @full_name, username = @username, role = @role, ministry = @ministry, password_hash = @password_hash where id = @id and is_active = true",
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("full_name", fullName.Trim());
        command.Parameters.AddWithValue("username", username.Trim());
        command.Parameters.AddWithValue("role", role.Trim());
        command.Parameters.AddWithValue("ministry", string.IsNullOrWhiteSpace(ministry) ? DBNull.Value : ministry.Trim());
        if (passwordHash is not null)
        {
            command.Parameters.AddWithValue("password_hash", passwordHash);
        }

        try
        {
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected == 1 ? (true, null) : (false, "Përdoruesi nuk u gjet ose është joaktiv.");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return (false, "Ky username ekziston tashmë.");
        }
    }

    public Task<(bool IsSuccess, string? Error)> DeactivateUser(string id, CancellationToken cancellationToken = default) =>
        ExecuteUpdate(
            "update users set is_active = false where id = @id and is_active = true",
            id,
            cancellationToken);

    public Task<(bool IsSuccess, string? Error)> ActivateUser(string id, CancellationToken cancellationToken = default) =>
        ExecuteUpdate(
            "update users set is_active = true where id = @id and is_active = false",
            id,
            cancellationToken,
            inactiveMessage: "Përdoruesi nuk u gjet ose është aktiv.");

    private async Task<(bool IsSuccess, string? Error)> ExecuteUpdate(
        string sql,
        string id,
        CancellationToken cancellationToken,
        Action<NpgsqlCommand>? configure = null,
        string inactiveMessage = "Përdoruesi nuk u gjet ose është joaktiv.")
    {
        if (_connectionString is null)
        {
            return (false, "Databaza e përdoruesve nuk është konfiguruar.");
        }

        await using var connection = await OpenConnection(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        configure?.Invoke(command);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected == 1 ? (true, null) : (false, inactiveMessage);
    }

    private async Task<NpgsqlConnection> OpenConnection(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        return connection;
    }

    private async Task EnsureSchemaAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            create table if not exists users (
                id text primary key,
                username text not null,
                password_hash text not null,
                role text not null,
                ministry text null,
                full_name text not null,
                created_at timestamptz not null default now(),
                is_active boolean not null default true
            );
            create unique index if not exists ux_users_username_lower on users (lower(username));
            """,
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await SeedBootstrapUserAsync(
            connection,
            _bootstrapAdminUsername,
            _bootstrapAdminPasswordHash,
            ApplicationRoles.Admin,
            cancellationToken);

        await using var publicInnovationDirectorCommand = new NpgsqlCommand(
            """
            insert into users (id, username, password_hash, role, ministry, full_name, created_at, is_active)
            values (@id, @username, @password_hash, @role, null, @full_name, now(), true)
            on conflict (lower(username)) do update
            set password_hash = excluded.password_hash,
                role = excluded.role,
                ministry = null,
                full_name = excluded.full_name
            """,
            connection);
        publicInnovationDirectorCommand.Parameters.AddWithValue("id", $"usr-{Guid.NewGuid():N}");
        publicInnovationDirectorCommand.Parameters.AddWithValue("username", "drejtor.inovacioni");
        publicInnovationDirectorCommand.Parameters.AddWithValue("password_hash", BCrypt.Net.BCrypt.HashPassword("drejtor123"));
        publicInnovationDirectorCommand.Parameters.AddWithValue("role", ApplicationRoles.DrejtorInovacioniPublik);
        publicInnovationDirectorCommand.Parameters.AddWithValue("full_name", ApplicationRoles.ToDisplayLabel(ApplicationRoles.DrejtorInovacioniPublik));
        await publicInnovationDirectorCommand.ExecuteNonQueryAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(_bootstrapDirectorUsername) || string.IsNullOrWhiteSpace(_bootstrapDirectorPasswordHash))
        {
            return;
        }

        await using var seedCommand = new NpgsqlCommand(
            """
            insert into users (id, username, password_hash, role, ministry, full_name, created_at, is_active)
            values (@id, @username, @password_hash, @role, null, @full_name, now(), true)
            on conflict (lower(username)) do nothing
            """,
            connection);
        seedCommand.Parameters.AddWithValue("id", $"usr-{Guid.NewGuid():N}");
        seedCommand.Parameters.AddWithValue("username", _bootstrapDirectorUsername.Trim());
        seedCommand.Parameters.AddWithValue("password_hash", _bootstrapDirectorPasswordHash.Trim());
        seedCommand.Parameters.AddWithValue("role", ApplicationRoles.DrejtorAgjencie);
        seedCommand.Parameters.AddWithValue("full_name", ApplicationRoles.ToDisplayLabel(ApplicationRoles.DrejtorAgjencie));
        await seedCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SeedBootstrapUserAsync(
        NpgsqlConnection connection,
        string? username,
        string? passwordHash,
        string role,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return;
        }

        await using var command = new NpgsqlCommand(
            """
            insert into users (id, username, password_hash, role, ministry, full_name, created_at, is_active)
            values (@id, @username, @password_hash, @role, null, @full_name, now(), true)
            on conflict (lower(username)) do update
            set password_hash = excluded.password_hash,
                role = excluded.role,
                ministry = null,
                full_name = excluded.full_name
            """,
            connection);
        command.Parameters.AddWithValue("id", $"usr-{Guid.NewGuid():N}");
        command.Parameters.AddWithValue("username", username.Trim());
        command.Parameters.AddWithValue("password_hash", passwordHash.Trim());
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("full_name", ApplicationRoles.ToDisplayLabel(role));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static StoredUser ReadStoredUser(NpgsqlDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetString(5),
            reader.GetFieldValue<DateTimeOffset>(6),
            reader.GetBoolean(7));

    private static ManagedUserResponse ReadManagedUser(NpgsqlDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetString(4),
            reader.GetFieldValue<DateTimeOffset>(5),
            reader.GetBoolean(6));

    private static bool TryGetUsername(string username, out string normalizedUsername)
    {
        normalizedUsername = username?.Trim() ?? string.Empty;
        return normalizedUsername.Length > 0;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

        if (!configured.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !configured.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return configured;
        }

        var uri = new Uri(configured);
        var userInfo = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
            Password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty),
            SslMode = SslMode.Require
        }.ConnectionString;
    }
}
