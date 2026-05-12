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

        api.MapPost("/updates", (ClaimsPrincipal user, CreateWeeklyUpdateRequest request, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryCreateWeeklyUpdate(context, request, out var update, out var error)
                ? Results.Ok(update)
                : Results.BadRequest(new ApiErrorResponse("update_create_failed", error!));
        });

        api.MapGet("/change-proposals", (ClaimsPrincipal user, string? projectId, IUserContextService contextService, IUpdateService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetChangeProposals(context, projectId))
                : errorResult!;
        });

        api.MapPost("/change-proposals", (ClaimsPrincipal user, CreateProjectChangeProposalRequest request, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryCreateChangeProposal(context, request, out var proposal, out var error)
                ? Results.Ok(proposal)
                : Results.BadRequest(new ApiErrorResponse("change_proposal_failed", error!));
        });

        api.MapPatch("/change-proposals/{id}", (string id, ClaimsPrincipal user, ResolveChangeProposalRequest request, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManagePortfolio(context.Role))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return service.TryResolveChangeProposal(context, id, request.Action, out var proposal, out var error)
                ? Results.Ok(proposal)
                : Results.BadRequest(new ApiErrorResponse("change_proposal_resolution_failed", error!));
        });

        return api;
    }
}
