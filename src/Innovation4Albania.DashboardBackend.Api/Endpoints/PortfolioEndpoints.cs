using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static RouteGroupBuilder MapPortfolioEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/portfolio/okr", (ClaimsPrincipal user, IUserContextService contextService, IPortfolioService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetPortfolioOkr(context))
                : errorResult!;
        });

        api.MapPost("/portfolio/okr", (ClaimsPrincipal user, CreatePortfolioObjectiveRequest request, IUserContextService contextService, IPortfolioService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryCreatePortfolioObjective(context, request, out var objective, out var error)
                ? Results.Ok(objective)
                : Results.BadRequest(new ApiErrorResponse("portfolio_create_failed", error!));
        });

        return api;
    }
}
