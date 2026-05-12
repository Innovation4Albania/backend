using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/dashboard/summary", (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetSummary(context))
                : errorResult!;
        });

        api.MapGet("/dashboard/status-distribution", (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetStatusDistribution(context))
                : errorResult!;
        });

        api.MapGet("/dashboard/performance", (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetPerformance(context))
                : errorResult!;
        });

        api.MapGet("/dashboard/trend", (int? months, IDashboardService service) =>
            Results.Ok(service.GetTrend(Math.Clamp(months.GetValueOrDefault(12), 3, 24))));

        api.MapGet("/dashboard/ministry-distribution", (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetMinistryDistribution(context))
                : errorResult!;
        });

        api.MapGet("/dashboard/resource-capacity", (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetResourceCapacity(context))
                : errorResult!;
        });

        api.MapGet("/performance/board", (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetPerformanceBoard(context))
                : errorResult!;
        });

        api.MapGet("/risk-deviations", (ClaimsPrincipal user, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetRiskDeviations(context))
                : errorResult!;
        });

        return api;
    }
}
