using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static RouteGroupBuilder MapPortfolioEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/portfolio/okr", async (ClaimsPrincipal user, IUserContextService contextService, IPortfolioService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetPortfolioOkr(context));
        });

        api.MapPost("/portfolio/okr", async (ClaimsPrincipal user, CreatePortfolioObjectiveRequest request, IUserContextService contextService, IPortfolioService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryCreatePortfolioObjectiveAsync(context, request);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Portfolio objective {ObjectiveId} created by role {Role}.", result.Response!.Id, context.Role);
            }

            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("portfolio_create_failed", result.Error!));
        });

        api.MapPut("/portfolio/okr/{id}", async (string id, ClaimsPrincipal user, CreatePortfolioObjectiveRequest request, IUserContextService contextService, IPortfolioService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryUpdatePortfolioObjectiveAsync(context, id, request);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Portfolio objective {ObjectiveId} updated by role {Role}.", id, context.Role);
            }

            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("portfolio_update_failed", result.Error!));
        });

        api.MapDelete("/portfolio/okr/{id}", async (string id, ClaimsPrincipal user, IUserContextService contextService, IPortfolioService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryDeletePortfolioObjectiveAsync(context, id);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Portfolio objective {ObjectiveId} deleted by role {Role}.", id, context.Role);
            }

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ApiErrorResponse("portfolio_delete_failed", result.Error!));
        });

        return api;
    }
}
