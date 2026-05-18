using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class ProjectService(IInnovationDashboardRepository repository) : IProjectService
{
    public IReadOnlyList<ProjectResponse> GetProjects(UserContext context, string? status, string? query) => repository.GetProjects(context, status, query);
    public ProjectResponse? GetProjectById(string id, UserContext context) => repository.GetProjectById(id, context);
    public Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryCreateProjectAsync(UserContext context, CreateProjectRequest request) => repository.TryCreateProjectAsync(context, request);
    public Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryUpdateProjectAsync(UserContext context, string id, CreateProjectRequest request) => repository.TryUpdateProjectAsync(context, id, request);
    public Task<(bool IsSuccess, string? Error)> TryDeleteProjectAsync(UserContext context, string id) => repository.TryDeleteProjectAsync(context, id);
    public IReadOnlyList<ProjectEventResponse> GetProjectEvents(string id, UserContext context) => repository.GetEventsForProject(id, context);
    public Task<AiInsightResponse?> GetProjectAiInsights(string id, UserContext context, string apiKey) => repository.GetProjectAiInsights(id, context, apiKey);
}
