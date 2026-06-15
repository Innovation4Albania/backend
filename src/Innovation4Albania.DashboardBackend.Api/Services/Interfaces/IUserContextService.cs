using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IUserContextService
{
    bool TryCreateContext(string role, string? ministry, string? username, string? fullName, string? userId, string? programKey, out UserContext context, out string? error);
}
