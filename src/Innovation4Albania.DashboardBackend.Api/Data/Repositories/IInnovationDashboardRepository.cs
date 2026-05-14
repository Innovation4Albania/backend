using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Data.Repositories;

public interface IInnovationDashboardRepository
{
    IReadOnlyList<string> GetMinistries();
    string? ValidateLogin(LoginRequest request);
    bool IsValidContext(UserContext context, out string? error);
    UserResponse Login(LoginRequest request);
    DashboardSummaryResponse GetDashboardSummary(UserContext context);
    IReadOnlyList<StatusDistributionItem> GetStatusDistribution(UserContext context);
    IReadOnlyList<MinistryDistributionItem> GetMinistryDistribution(UserContext context);
    ResourceCapacitySummaryResponse GetResourceCapacitySummary(UserContext context);
    IReadOnlyList<PerformanceScoreItem> GetPerformanceScores(UserContext context);
    IReadOnlyList<TrendPointResponse> GetTrend(int months);
    IReadOnlyList<ProjectResponse> GetProjects(UserContext context, string? status, string? query);
    ProjectResponse? GetProjectById(string id, UserContext context);
    bool TryCreateProject(UserContext context, CreateProjectRequest request, out ProjectResponse? response, out string? error);
    bool TryUpdateProject(UserContext context, string id, CreateProjectRequest request, out ProjectResponse? response, out string? error);
    bool TryDeleteProject(UserContext context, string id, out string? error);
    IReadOnlyList<ProjectEventResponse> GetEventsForProject(string projectId, UserContext context);
    Task<AiInsightResponse?> GetProjectAiInsights(string projectId, UserContext context, string apiKey);
    IReadOnlyList<PerformanceBoardColumnResponse> GetPerformanceBoard(UserContext context);
    PortfolioOkrResponse GetPortfolioOkr(UserContext context);
    bool TryCreatePortfolioObjective(UserContext context, CreatePortfolioObjectiveRequest request, out ObjectiveResponse? response, out string? error);
    bool TryUpdatePortfolioObjective(UserContext context, string id, CreatePortfolioObjectiveRequest request, out ObjectiveResponse? response, out string? error);
    bool TryDeletePortfolioObjective(UserContext context, string id, out string? error);
    IReadOnlyList<RiskDeviationResponse> GetRiskDeviations(UserContext context);
    IReadOnlyList<WeeklyUpdateResponse> GetWeeklyUpdates(UserContext context, string? projectId);
    bool TryCreateWeeklyUpdate(UserContext context, CreateWeeklyUpdateRequest request, out WeeklyUpdateResponse? response, out string? error);
    IReadOnlyList<ProjectChangeProposalResponse> GetChangeProposals(UserContext context, string? projectId);
    bool TryCreateChangeProposal(UserContext context, CreateProjectChangeProposalRequest request, out ProjectChangeProposalResponse? response, out string? error);
    bool TryResolveChangeProposal(UserContext context, string id, string action, out ProjectChangeProposalResponse? response, out string? error);
    CalendarMonthResponse GetCalendarMonth(UserContext context, DateOnly month);
    IReadOnlyList<UpcomingEventResponse> GetUpcomingEvents(UserContext context, int limit);
    IReadOnlyList<UpcomingEventResponse> GetPastEvents(UserContext context, int limit);
    Task<AiChatResponse> GetAiChatReply(UserContext context, AiChatRequest request, string apiKey);
}
