using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IAuthService
{
    string? ValidateLogin(LoginRequest request);
    string? ValidateViewLink(LoginRequest request);
    AuthResponse Login(LoginRequest request);
    AuthResponse CreateViewLinkSession(LoginRequest request);
    string RefreshToken(UserContext context);
}
