using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IUpdateService
{
    IReadOnlyList<WeeklyUpdateResponse> GetWeeklyUpdates(UserContext context, string? projectId);
    bool TryCreateWeeklyUpdate(UserContext context, CreateWeeklyUpdateRequest request, out WeeklyUpdateResponse? response, out string? error);
    IReadOnlyList<ProjectChangeProposalResponse> GetChangeProposals(UserContext context, string? projectId);
    bool TryCreateChangeProposal(UserContext context, CreateProjectChangeProposalRequest request, out ProjectChangeProposalResponse? response, out string? error);
}
