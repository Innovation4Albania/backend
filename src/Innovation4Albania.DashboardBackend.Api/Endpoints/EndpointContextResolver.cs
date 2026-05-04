using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

internal static class EndpointContextResolver
{
    public static bool TryResolve(string role, string? ministry, IUserContextService contextService, out UserContext context, out IResult? errorResult)
    {
        if (contextService.TryCreateContext(role, ministry, out context, out var error))
        {
            errorResult = null;
            return true;
        }

        errorResult = Results.BadRequest(new ApiErrorResponse("validation_error", error!));
        return false;
    }
}
