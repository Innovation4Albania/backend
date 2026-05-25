using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class UpdateService(IInnovationDashboardRepository repository) : IUpdateService
{
    public Task<IReadOnlyList<WeeklyUpdateResponse>> GetWeeklyUpdates(UserContext context, string? projectId) => repository.GetWeeklyUpdates(context, projectId);
    public Task<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)> TryCreateWeeklyUpdateAsync(UserContext context, CreateWeeklyUpdateRequest request) => repository.TryCreateWeeklyUpdateAsync(context, request);
    public Task<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)> TryUpdateWeeklyUpdateAsync(UserContext context, string id, CreateWeeklyUpdateRequest request) => repository.TryUpdateWeeklyUpdateAsync(context, id, request);
    public Task<(bool IsSuccess, string? Error)> TryDeleteWeeklyUpdateAsync(UserContext context, string id) => repository.TryDeleteWeeklyUpdateAsync(context, id);
    public Task<IReadOnlyList<ProjectChangeProposalResponse>> GetChangeProposals(UserContext context, string? projectId) => repository.GetChangeProposals(context, projectId);
    public Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryCreateChangeProposalAsync(UserContext context, CreateProjectChangeProposalRequest request) => repository.TryCreateChangeProposalAsync(context, request);
    public Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryResolveChangeProposalAsync(UserContext context, string id, string action) => repository.TryResolveChangeProposalAsync(context, id, action);
    public Task<(bool IsSuccess, string? Error)> TryDeleteChangeProposalAsync(UserContext context, string id) => repository.TryDeleteChangeProposalAsync(context, id);
}
