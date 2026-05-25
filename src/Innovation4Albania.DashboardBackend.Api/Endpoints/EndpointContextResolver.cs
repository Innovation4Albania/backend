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
        var username = user.FindFirstValue("username");

        if (string.IsNullOrWhiteSpace(role))
        {
            context = new UserContext(string.Empty, null);
            errorResult = Results.Json(
                new ApiErrorResponse("unauthorized", "Sesioni nuk është i vlefshëm ose ka skaduar."),
                statusCode: StatusCodes.Status401Unauthorized);
            return false;
        }

        if (contextService.TryCreateContext(role, ministry, username, out context, out var error))
        {
            errorResult = null;
            return true;
        }

        errorResult = Results.BadRequest(new ApiErrorResponse("validation_error", error!));
        return false;
    }
}
