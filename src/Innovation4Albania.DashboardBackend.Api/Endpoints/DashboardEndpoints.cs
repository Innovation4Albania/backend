using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/dashboard/summary", (string role, string? ministry, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetSummary(context))
                : errorResult!;
        });

        api.MapGet("/dashboard/status-distribution", (string role, string? ministry, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetStatusDistribution(context))
                : errorResult!;
        });

        api.MapGet("/dashboard/performance", (string role, string? ministry, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetPerformance(context))
                : errorResult!;
        });

        api.MapGet("/dashboard/trend", (int? months, IDashboardService service) =>
            Results.Ok(service.GetTrend(Math.Clamp(months.GetValueOrDefault(12), 3, 24))));

        api.MapGet("/dashboard/ministry-distribution", (string role, string? ministry, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetMinistryDistribution(context))
                : errorResult!;
        });

        api.MapGet("/dashboard/resource-capacity", (string role, string? ministry, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetResourceCapacity(context))
                : errorResult!;
        });

        api.MapGet("/performance/board", (string role, string? ministry, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetPerformanceBoard(context))
                : errorResult!;
        });

        api.MapGet("/risk-deviations", (string role, string? ministry, IUserContextService contextService, IDashboardService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetRiskDeviations(context))
                : errorResult!;
        });

        return api;
    }
}
