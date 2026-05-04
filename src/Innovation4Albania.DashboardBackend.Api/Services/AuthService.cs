using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class AuthService(IInnovationDashboardRepository repository) : IAuthService
{
    public string? ValidateLogin(LoginRequest request) => repository.ValidateLogin(request);
    public UserResponse Login(LoginRequest request) => repository.Login(request);
}
