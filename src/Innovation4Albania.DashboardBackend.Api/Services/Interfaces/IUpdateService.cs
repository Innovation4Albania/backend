using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IUpdateService
{
    Task<IReadOnlyList<WeeklyUpdateResponse>> GetWeeklyUpdates(UserContext context, string? projectId);
    Task<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)> TryCreateWeeklyUpdateAsync(UserContext context, CreateWeeklyUpdateRequest request);
    Task<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)> TryUpdateWeeklyUpdateAsync(UserContext context, string id, CreateWeeklyUpdateRequest request);
    Task<(bool IsSuccess, string? Error)> TryDeleteWeeklyUpdateAsync(UserContext context, string id);
    Task<IReadOnlyList<ProjectChangeProposalResponse>> GetChangeProposals(UserContext context, string? projectId);
    Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryCreateChangeProposalAsync(UserContext context, CreateProjectChangeProposalRequest request);
    Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryResolveChangeProposalAsync(UserContext context, string id, string action);
}
