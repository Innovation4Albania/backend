using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class UpdateEndpoints
{
    public static RouteGroupBuilder MapUpdateEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/updates", (string role, string? ministry, string? projectId, IUserContextService contextService, IUpdateService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetWeeklyUpdates(context, projectId))
                : errorResult!;
        });

        api.MapPost("/updates", (string role, string? ministry, CreateWeeklyUpdateRequest request, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryCreateWeeklyUpdate(context, request, out var update, out var error)
                ? Results.Ok(update)
                : Results.BadRequest(new ApiErrorResponse("update_create_failed", error!));
        });

        api.MapGet("/change-proposals", (string role, string? ministry, string? projectId, IUserContextService contextService, IUpdateService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetChangeProposals(context, projectId))
                : errorResult!;
        });

        api.MapPost("/change-proposals", (string role, string? ministry, CreateProjectChangeProposalRequest request, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryCreateChangeProposal(context, request, out var proposal, out var error)
                ? Results.Ok(proposal)
                : Results.BadRequest(new ApiErrorResponse("change_proposal_failed", error!));
        });

        return api;
    }
}
