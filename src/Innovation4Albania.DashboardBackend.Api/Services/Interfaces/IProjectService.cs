using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IProjectService
{
    IReadOnlyList<ProjectResponse> GetProjects(UserContext context, string? status, string? query);
    ProjectResponse? GetProjectById(string id, UserContext context);
    bool TryCreateProject(UserContext context, CreateProjectRequest request, out ProjectResponse? response, out string? error);
    bool TryUpdateProject(UserContext context, string id, CreateProjectRequest request, out ProjectResponse? response, out string? error);
    IReadOnlyList<ProjectEventResponse> GetProjectEvents(string id, UserContext context);
    AiInsightResponse? GetProjectAiInsights(string id, UserContext context);
}
