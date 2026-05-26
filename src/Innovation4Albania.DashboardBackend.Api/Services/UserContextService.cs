using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class UserContextService(IInnovationDashboardRepository repository) : Interfaces.IUserContextService
{
    public bool TryCreateContext(string role, string? ministry, string? username, string? fullName, string? userId, out UserContext context, out string? error)
    {
        context = UserContext.From(role, ministry, username, fullName, userId);
        return repository.IsValidContext(context, out error);
    }
}
