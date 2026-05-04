using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/projects", (string role, string? ministry, string? status, string? query, IUserContextService contextService, IProjectService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetProjects(context, status, query))
                : errorResult!;
        });

        api.MapGet("/projects/{id}", (string id, string role, string? ministry, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var project = service.GetProjectById(id, context);
            return project is null
                ? Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."))
                : Results.Ok(project);
        });

        api.MapPost("/projects", (string role, string? ministry, CreateProjectRequest request, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryCreateProject(context, request, out var project, out var error)
                ? Results.Ok(project)
                : Results.BadRequest(new ApiErrorResponse("project_create_failed", error!));
        });

        api.MapPut("/projects/{id}", (string id, string role, string? ministry, CreateProjectRequest request, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            return service.TryUpdateProject(context, id, request, out var project, out var error)
                ? Results.Ok(project)
                : Results.BadRequest(new ApiErrorResponse("project_update_failed", error!));
        });

        api.MapGet("/projects/{id}/events", (string id, string role, string? ministry, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (service.GetProjectById(id, context) is null)
            {
                return Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."));
            }

            return Results.Ok(service.GetProjectEvents(id, context));
        });

        api.MapGet("/projects/{id}/ai-insights", (string id, string role, string? ministry, IUserContextService contextService, IProjectService service) =>
        {
            if (!EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var insights = service.GetProjectAiInsights(id, context);
            return insights is null
                ? Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."))
                : Results.Ok(insights);
        });

        return api;
    }
}
