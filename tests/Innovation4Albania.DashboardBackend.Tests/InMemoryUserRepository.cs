using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

internal sealed class InMemoryUserRepository(params StoredUser[] initialUsers) : IUserRepository
{
    private readonly List<StoredUser> _users = [.. initialUsers];

    public Task<StoredUser?> GetUserByUsername(string username, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)));

    public Task<StoredUser?> GetUserById(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

    public Task<IReadOnlyList<ManagedUserResponse>> GetUsers(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ManagedUserResponse>>(_users.Select(ToResponse).ToList());

    public Task<(bool IsSuccess, ManagedUserResponse? Response, string? Error)> CreateUser(
        CreateUserRequest request,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        if (_users.Any(user => string.Equals(user.Username, request.Username, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult<(bool, ManagedUserResponse?, string?)>((false, null, "Ky username ekziston tashmë."));
        }

        var user = new StoredUser(
            $"usr-{_users.Count + 1}",
            request.Username,
            passwordHash,
            request.Role,
            request.Ministry,
            request.FullName,
            DateTimeOffset.UtcNow,
            true);
        _users.Add(user);
        return Task.FromResult<(bool, ManagedUserResponse?, string?)>((true, ToResponse(user), null));
    }

    public Task<(bool IsSuccess, string? Error)> UpdatePassword(string id, string passwordHash, CancellationToken cancellationToken = default)
    {
        var index = _users.FindIndex(user => user.Id == id && user.IsActive);
        if (index < 0)
        {
            return Task.FromResult((false, (string?)"Përdoruesi nuk u gjet ose është joaktiv."));
        }

        _users[index] = _users[index] with { PasswordHash = passwordHash };
        return Task.FromResult((true, (string?)null));
    }

    public Task<(bool IsSuccess, string? Error)> UpdateCredentials(string id, string username, string? passwordHash, CancellationToken cancellationToken = default)
    {
        if (_users.Any(user => user.Id != id && string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult((false, (string?)"Ky username ekziston tashmë."));
        }

        var index = _users.FindIndex(user => user.Id == id && user.IsActive);
        if (index < 0)
        {
            return Task.FromResult((false, (string?)"Përdoruesi nuk u gjet ose është joaktiv."));
        }

        _users[index] = _users[index] with
        {
            Username = username,
            PasswordHash = passwordHash ?? _users[index].PasswordHash
        };
        return Task.FromResult((true, (string?)null));
    }

    public Task<(bool IsSuccess, string? Error)> UpdateUser(string id, string fullName, string username, string role, string? ministry, string? passwordHash, CancellationToken cancellationToken = default)
    {
        if (_users.Any(user => user.Id != id && string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult((false, (string?)"Ky username ekziston tashmë."));
        }

        var index = _users.FindIndex(user => user.Id == id && user.IsActive);
        if (index < 0)
        {
            return Task.FromResult((false, (string?)"Përdoruesi nuk u gjet ose është joaktiv."));
        }

        _users[index] = _users[index] with
        {
            FullName = fullName.Trim(),
            Username = username.Trim(),
            Role = role.Trim(),
            Ministry = string.IsNullOrWhiteSpace(ministry) ? null : ministry.Trim(),
            PasswordHash = passwordHash ?? _users[index].PasswordHash
        };
        return Task.FromResult((true, (string?)null));
    }

    public Task<(bool IsSuccess, string? Error)> DeactivateUser(string id, CancellationToken cancellationToken = default)
    {
        var index = _users.FindIndex(user => user.Id == id && user.IsActive);
        if (index < 0)
        {
            return Task.FromResult((false, (string?)"Përdoruesi nuk u gjet ose është joaktiv."));
        }

        _users[index] = _users[index] with { IsActive = false };
        return Task.FromResult((true, (string?)null));
    }

    public Task<(bool IsSuccess, string? Error)> ActivateUser(string id, CancellationToken cancellationToken = default)
    {
        var index = _users.FindIndex(user => user.Id == id && !user.IsActive);
        if (index < 0)
        {
            return Task.FromResult((false, (string?)"PÃ«rdoruesi nuk u gjet ose Ã«shtÃ« aktiv."));
        }

        _users[index] = _users[index] with { IsActive = true };
        return Task.FromResult((true, (string?)null));
    }

    public Task<(bool IsSuccess, string? Error)> DeleteUser(string id, CancellationToken cancellationToken = default)
    {
        var index = _users.FindIndex(user => user.Id == id);
        if (index < 0)
        {
            return Task.FromResult((false, (string?)"Përdoruesi nuk u gjet."));
        }

        _users.RemoveAt(index);
        return Task.FromResult((true, (string?)null));
    }

    public static StoredUser Account(
        string id,
        string username,
        string password,
        string role,
        string fullName,
        string? ministry = null) =>
        new(id, username, BCrypt.Net.BCrypt.HashPassword(password), role, ministry, fullName, DateTimeOffset.UtcNow, true);

    private static ManagedUserResponse ToResponse(StoredUser user) =>
        new(user.Id, user.Username, user.Role, user.Ministry, user.FullName, user.CreatedAt, user.IsActive);
}
