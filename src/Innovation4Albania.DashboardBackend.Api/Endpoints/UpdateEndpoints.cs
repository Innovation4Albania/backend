using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class UpdateEndpoints
{
    public static RouteGroupBuilder MapUpdateEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/updates", async (ClaimsPrincipal user, string? projectId, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetWeeklyUpdates(context, projectId));
        });

        api.MapPost("/updates", async (ClaimsPrincipal user, CreateWeeklyUpdateRequest request, IUserContextService contextService, IUpdateService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryCreateWeeklyUpdateAsync(context, request);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Weekly update {UpdateId} submitted for project {ProjectId} by role {Role}.", result.Response!.Id, request.ProjectId, context.Role);
            }

            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("update_create_failed", result.Error!));
        });

        api.MapPut("/updates/{id}", async (string id, ClaimsPrincipal user, CreateWeeklyUpdateRequest request, IUserContextService contextService, IUpdateService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryUpdateWeeklyUpdateAsync(context, id, request);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Weekly update {UpdateId} updated by role {Role}.", id, context.Role);
            }

            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("update_edit_failed", result.Error!));
        });

        api.MapDelete("/updates/{id}", async (string id, ClaimsPrincipal user, IUserContextService contextService, IUpdateService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryDeleteWeeklyUpdateAsync(context, id);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Weekly update {UpdateId} deleted by role {Role}.", id, context.Role);
            }

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ApiErrorResponse("update_delete_failed", result.Error!));
        });

        api.MapGet("/change-proposals", async (ClaimsPrincipal user, string? projectId, IUserContextService contextService, IUpdateService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetChangeProposals(context, projectId));
        });

        api.MapPost("/change-proposals", async (ClaimsPrincipal user, CreateProjectChangeProposalRequest request, IUserContextService contextService, IUpdateService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryCreateChangeProposalAsync(context, request);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Change proposal {ProposalId} submitted for project {ProjectId} by role {Role}.", result.Response!.Id, request.ProjectId, context.Role);
            }

            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("change_proposal_failed", result.Error!));
        });

        api.MapPatch("/change-proposals/{id}", async (string id, ClaimsPrincipal user, ResolveChangeProposalRequest request, IUserContextService contextService, IUpdateService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManagePortfolio(context.Role))
            {
                return Results.Json(
                    new ApiErrorResponse("forbidden", "Ky rol nuk ka leje të procesojë propozime ndryshimi."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await service.TryResolveChangeProposalAsync(context, id, request.Action);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Change proposal {ProposalId} resolved with action {Action} by role {Role}.", id, request.Action, context.Role);
            }

            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("change_proposal_resolution_failed", result.Error!));
        });

        api.MapDelete("/change-proposals/{id}", async (string id, ClaimsPrincipal user, IUserContextService contextService, IUpdateService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanDeleteChangeProposals(context.Role))
            {
                return Results.Json(
                    new ApiErrorResponse("forbidden", "Ky rol nuk ka leje të fshijë propozime ndryshimi."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await service.TryDeleteChangeProposalAsync(context, id);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Change proposal {ProposalId} deleted by role {Role}.", id, context.Role);
            }

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ApiErrorResponse("change_proposal_delete_failed", result.Error!));
        });

        return api;
    }
}
