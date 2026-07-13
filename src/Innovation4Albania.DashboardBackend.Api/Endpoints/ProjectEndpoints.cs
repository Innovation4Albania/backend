using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/projects", async (ClaimsPrincipal user, string? status, string? query, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetProjects(context, status, query));
        });

        api.MapGet("/projects/{id}", async (string id, ClaimsPrincipal user, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var project = await service.GetProjectById(id, context);
            return project is null
                ? Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."))
                : Results.Ok(project);
        });

        api.MapPost("/projects", async (ClaimsPrincipal user, CreateProjectRequest request, IUserContextService contextService, IProjectService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanCreateProjects(context.Role))
            {
                return Results.Json(
                    new ApiErrorResponse("forbidden", "Ky rol nuk ka leje të krijojë projekte."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await service.TryCreateProjectAsync(context, request);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Project {ProjectId} created by role {Role}.", result.Response!.Id, context.Role);
            }

            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("project_create_failed", result.Error!));
        });

        api.MapPut("/projects/{id}", async (string id, ClaimsPrincipal user, CreateProjectRequest request, IUserContextService contextService, IProjectService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryUpdateProjectAsync(context, id, request);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Project {ProjectId} updated by role {Role}.", id, context.Role);
            }

            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("project_update_failed", result.Error!));
        });

        api.MapDelete("/projects/{id}", async (string id, ClaimsPrincipal user, IUserContextService contextService, IProjectService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await service.TryDeleteProjectAsync(context, id);
            if (result.IsSuccess)
            {
                auditLogger.LogInformation("Project {ProjectId} deleted by role {Role}.", id, context.Role);
            }

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ApiErrorResponse("project_delete_failed", result.Error!));
        });

        api.MapGet("/projects/{id}/events", async (string id, ClaimsPrincipal user, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (await service.GetProjectById(id, context) is null)
            {
                return Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."));
            }

            return Results.Ok(await service.GetProjectEvents(id, context));
        });

        api.MapGet("/projects/{id}/ai-insights", async (string id, ClaimsPrincipal user,
            IUserContextService contextService, IProjectService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            var insights = await service.GetProjectAiInsights(id, context);
            if (insights is not null)
            {
                auditLogger.LogInformation("AI insights generated for project {ProjectId} by role {Role}.", id, context.Role);
            }

            return insights is null
                ? Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet."))
                : Results.Ok(insights);
        }).RequireRateLimiting("ai-insights");

        return api;
    }
}
