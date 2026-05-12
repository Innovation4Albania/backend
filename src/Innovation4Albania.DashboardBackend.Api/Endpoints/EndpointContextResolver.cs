using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

internal static class EndpointContextResolver
{
    public static bool TryResolve(ClaimsPrincipal user, IUserContextService contextService, out UserContext context, out IResult? errorResult)
    {
        var role = user.FindFirstValue("role");
        var ministry = user.FindFirstValue("ministry");

        if (string.IsNullOrWhiteSpace(role))
        {
            context = new UserContext(string.Empty, null);
            errorResult = Results.Unauthorized();
            return false;
        }

        if (contextService.TryCreateContext(role, ministry, out context, out var error))
        {
            errorResult = null;
            return true;
        }

        errorResult = Results.BadRequest(new ApiErrorResponse("validation_error", error!));
        return false;
    }
}
