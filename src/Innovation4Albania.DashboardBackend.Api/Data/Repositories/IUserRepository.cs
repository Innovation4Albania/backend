using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Data.Repositories;

public sealed record StoredUser(
    string Id,
    string Username,
    string PasswordHash,
    string Role,
    string? Ministry,
    string FullName,
    DateTimeOffset CreatedAt,
    bool IsActive);

public interface IUserRepository
{
    Task<StoredUser?> GetUserByUsername(string username, CancellationToken cancellationToken = default);
    Task<StoredUser?> GetUserById(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedUserResponse>> GetUsers(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedUserResponse>> GetManagedUsers(
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);
    Task<(bool IsSuccess, ManagedUserResponse? Response, string? Error)> CreateUser(
        CreateUserRequest request,
        string passwordHash,
        CancellationToken cancellationToken = default);
    Task<(bool IsSuccess, string? Error)> UpdatePassword(
        string id,
        string passwordHash,
        CancellationToken cancellationToken = default);
    Task<(bool IsSuccess, string? Error)> UpdateCredentials(
        string id,
        string username,
        string? passwordHash,
        CancellationToken cancellationToken = default);
    Task<(bool IsSuccess, string? Error)> UpdateUser(
        string id,
        string fullName,
        string username,
        string role,
        string? ministry,
        string? passwordHash,
        CancellationToken cancellationToken = default);
    Task<(bool IsSuccess, string? Error)> DeactivateUser(string id, CancellationToken cancellationToken = default);
    Task<(bool IsSuccess, string? Error)> ActivateUser(string id, CancellationToken cancellationToken = default);
    Task<(bool IsSuccess, string? Error)> DeleteUser(string id, CancellationToken cancellationToken = default);
}
