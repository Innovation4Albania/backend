using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IAuthService
{
    Task<(bool IsSuccess, AuthResponse? Response, string? Error)> TryLoginAsync(LoginRequest request);
    string? ValidateViewLink(LoginRequest request);
    Task<(bool IsSuccess, AuthResponse? Response, string? Error)> CreateViewLinkSessionAsync(LoginRequest request);
    Task<IReadOnlyList<ViewUserResponse>> GetViewUsersAsync(string role);
    Task<string?> RefreshTokenAsync(UserContext context);
    Task<IReadOnlyList<ManagedUserResponse>> GetManagedUsersAsync(UserContext context);
    Task<(bool IsSuccess, ManagedUserResponse? Response, string? Error)> CreateUserAsync(UserContext context, CreateUserRequest request);
    Task<(bool IsSuccess, ManagedUserResponse? Response, string? Error)> UpdateUserAsync(UserContext context, string id, UpdateManagedUserRequest request);
    Task<(bool IsSuccess, string? Error)> ResetPasswordAsync(UserContext context, string id, AdminResetPasswordRequest request);
    Task<(bool IsSuccess, string? Error)> DeactivateUserAsync(UserContext context, string id);
    Task<(bool IsSuccess, string? Error)> ActivateUserAsync(UserContext context, string id);
    Task<(bool IsSuccess, string? Error)> DeleteUserAsync(UserContext context, string id);
    Task<(bool IsSuccess, AuthResponse? Response, string? Error)> ChangeOwnCredentialsAsync(UserContext context, ChangeOwnCredentialsRequest request);
}
