using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/projects", (ClaimsPrincipal user, string? status, string? query, IUserContextService contextService, IProjectService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetProjects(context, status, query))
                : errorResult!;
        });

        api.MapGet("/projects/{id}", (string id, ClaimsPrincipal user, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var project = service.GetProjectById(id, context);
            return project is null
                ? Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."))
                : Results.Ok(project);
        });

        api.MapPost("/projects", async (ClaimsPrincipal user, CreateProjectRequest request, IUserContextService contextService, IProjectService service) =>
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
            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("project_create_failed", result.Error!));
        });

        api.MapPut("/projects/{id}", async (string id, ClaimsPrincipal user, CreateProjectRequest request, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanCreateProjects(context.Role))
            {
                return Results.Json(
                    new ApiErrorResponse("forbidden", "Ky rol nuk ka leje të editojë projekte."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await service.TryUpdateProjectAsync(context, id, request);
            return result.IsSuccess
                ? Results.Ok(result.Response)
                : Results.BadRequest(new ApiErrorResponse("project_update_failed", result.Error!));
        });

        api.MapDelete("/projects/{id}", async (string id, ClaimsPrincipal user, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanCreateProjects(context.Role))
            {
                return Results.Json(
                    new ApiErrorResponse("forbidden", "Ky rol nuk ka leje të fshijë projekte."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await service.TryDeleteProjectAsync(context, id);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ApiErrorResponse("project_delete_failed", result.Error!));
        });

        api.MapGet("/projects/{id}/events", (string id, ClaimsPrincipal user, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (service.GetProjectById(id, context) is null)
            {
                return Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."));
            }

            return Results.Ok(service.GetProjectEvents(id, context));
        });

        api.MapGet("/projects/{id}/ai-insights", async (string id, ClaimsPrincipal user,
            IUserContextService contextService, IProjectService service, IConfiguration configuration) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            var apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
            var insights = await service.GetProjectAiInsights(id, context, apiKey);
            return insights is null
                ? Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet."))
                : Results.Ok(insights);
        });

        return api;
    }
}
