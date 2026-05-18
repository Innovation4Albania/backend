using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class UpdateEndpoints
{
    public static RouteGroupBuilder MapUpdateEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/updates", (ClaimsPrincipal user, string? projectId, IUserContextService contextService, IUpdateService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetWeeklyUpdates(context, projectId))
                : errorResult!;
        });

        api.MapPost("/updates", async (ClaimsPrincipal user, CreateWeeklyUpdateRequest request, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryCreateWeeklyUpdateAsync(context, request);
            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("update_create_failed", result.Error!));
        });

        api.MapGet("/change-proposals", (ClaimsPrincipal user, string? projectId, IUserContextService contextService, IUpdateService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetChangeProposals(context, projectId))
                : errorResult!;
        });

        api.MapPost("/change-proposals", async (ClaimsPrincipal user, CreateProjectChangeProposalRequest request, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryCreateChangeProposalAsync(context, request);
            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("change_proposal_failed", result.Error!));
        });

        api.MapPatch("/change-proposals/{id}", async (string id, ClaimsPrincipal user, ResolveChangeProposalRequest request, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManagePortfolio(context.Role))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var result = await service.TryResolveChangeProposalAsync(context, id, request.Action);
            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("change_proposal_resolution_failed", result.Error!));
        });

        return api;
    }
}
