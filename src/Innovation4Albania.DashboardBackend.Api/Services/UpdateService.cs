using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class UpdateService(IInnovationDashboardRepository repository) : IUpdateService
{
    public IReadOnlyList<WeeklyUpdateResponse> GetWeeklyUpdates(UserContext context, string? projectId) => repository.GetWeeklyUpdates(context, projectId);
    public bool TryCreateWeeklyUpdate(UserContext context, CreateWeeklyUpdateRequest request, out WeeklyUpdateResponse? response, out string? error) => repository.TryCreateWeeklyUpdate(context, request, out response, out error);
    public IReadOnlyList<ProjectChangeProposalResponse> GetChangeProposals(UserContext context, string? projectId) => repository.GetChangeProposals(context, projectId);
    public bool TryCreateChangeProposal(UserContext context, CreateProjectChangeProposalRequest request, out ProjectChangeProposalResponse? response, out string? error) => repository.TryCreateChangeProposal(context, request, out response, out error);
}
