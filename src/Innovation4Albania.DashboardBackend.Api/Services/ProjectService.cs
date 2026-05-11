using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class ProjectService(IInnovationDashboardRepository repository) : IProjectService
{
    public IReadOnlyList<ProjectResponse> GetProjects(UserContext context, string? status, string? query) => repository.GetProjects(context, status, query);
    public ProjectResponse? GetProjectById(string id, UserContext context) => repository.GetProjectById(id, context);
    public bool TryCreateProject(UserContext context, CreateProjectRequest request, out ProjectResponse? response, out string? error) => repository.TryCreateProject(context, request, out response, out error);
    public bool TryUpdateProject(UserContext context, string id, CreateProjectRequest request, out ProjectResponse? response, out string? error) => repository.TryUpdateProject(context, id, request, out response, out error);
    public bool TryDeleteProject(UserContext context, string id, out string? error) => repository.TryDeleteProject(context, id, out error);
    public IReadOnlyList<ProjectEventResponse> GetProjectEvents(string id, UserContext context) => repository.GetEventsForProject(id, context);
    public Task<AiInsightResponse?> GetProjectAiInsights(string id, UserContext context, string apiKey) => repository.GetProjectAiInsights(id, context, apiKey);
}
