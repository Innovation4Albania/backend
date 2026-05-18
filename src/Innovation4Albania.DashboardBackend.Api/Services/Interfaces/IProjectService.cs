using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IProjectService
{
    IReadOnlyList<ProjectResponse> GetProjects(UserContext context, string? status, string? query);
    ProjectResponse? GetProjectById(string id, UserContext context);
    Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryCreateProjectAsync(UserContext context, CreateProjectRequest request);
    Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryUpdateProjectAsync(UserContext context, string id, CreateProjectRequest request);
    Task<(bool IsSuccess, string? Error)> TryDeleteProjectAsync(UserContext context, string id);
    IReadOnlyList<ProjectEventResponse> GetProjectEvents(string id, UserContext context);
    Task<AiInsightResponse?> GetProjectAiInsights(string id, UserContext context, string apiKey);
}
