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

        api.MapPut("/portfolio/okr/{id}", (string id, ClaimsPrincipal user, CreatePortfolioObjectiveRequest request, IUserContextService contextService, IPortfolioService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryUpdatePortfolioObjective(context, id, request, out var objective, out var error)
                ? Results.Ok(objective)
                : Results.BadRequest(new ApiErrorResponse("portfolio_update_failed", error!));
        });

        api.MapDelete("/portfolio/okr/{id}", (string id, ClaimsPrincipal user, IUserContextService contextService, IPortfolioService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryDeletePortfolioObjective(context, id, out var error)
                ? Results.NoContent()
                : Results.BadRequest(new ApiErrorResponse("portfolio_delete_failed", error!));
        });

        return api;
    }
}
