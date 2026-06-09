using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/dashboard/summary", async (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetSummary(context));
        });

        api.MapGet("/dashboard/status-distribution", async (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetStatusDistribution(context));
        });

        api.MapGet("/dashboard/performance", async (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetPerformance(context));
        });

        api.MapGet("/dashboard/trend", async (ClaimsPrincipal user, int? months, IUserContextService contextService, IDashboardService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetTrend(context, Math.Clamp(months.GetValueOrDefault(12), 3, 24)));
        });

        api.MapGet("/dashboard/ministry-distribution", async (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetMinistryDistribution(context));
        });

        api.MapGet("/dashboard/resource-capacity", async (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetResourceCapacity(context));
        });

        api.MapGet("/performance/board", async (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetPerformanceBoard(context));
        });

        api.MapGet("/risk-deviations", async (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            if (!ApplicationRoles.CanViewRiskDeviations(context.Role))
            {
                return Results.Json(
                    new ApiErrorResponse("forbidden", "Ky rol nuk ka leje te shikoje devijimet e riskut."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            return Results.Ok(await service.GetRiskDeviations(context));
        });

        return api;
    }
}
