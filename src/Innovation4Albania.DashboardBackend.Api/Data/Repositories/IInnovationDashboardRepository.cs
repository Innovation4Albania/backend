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
    Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryCreateProjectAsync(UserContext context, CreateProjectRequest request);
    Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryUpdateProjectAsync(UserContext context, string id, CreateProjectRequest request);
    Task<(bool IsSuccess, string? Error)> TryDeleteProjectAsync(UserContext context, string id);
    IReadOnlyList<ProjectEventResponse> GetEventsForProject(string projectId, UserContext context);
    Task<AiInsightResponse?> GetProjectAiInsights(string projectId, UserContext context, string apiKey);
    IReadOnlyList<PerformanceBoardColumnResponse> GetPerformanceBoard(UserContext context);
    PortfolioOkrResponse GetPortfolioOkr(UserContext context);
    Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryCreatePortfolioObjectiveAsync(UserContext context, CreatePortfolioObjectiveRequest request);
    Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryUpdatePortfolioObjectiveAsync(UserContext context, string id, CreatePortfolioObjectiveRequest request);
    Task<(bool IsSuccess, string? Error)> TryDeletePortfolioObjectiveAsync(UserContext context, string id);
    IReadOnlyList<RiskDeviationResponse> GetRiskDeviations(UserContext context);
    IReadOnlyList<WeeklyUpdateResponse> GetWeeklyUpdates(UserContext context, string? projectId);
    Task<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)> TryCreateWeeklyUpdateAsync(UserContext context, CreateWeeklyUpdateRequest request);
    IReadOnlyList<ProjectChangeProposalResponse> GetChangeProposals(UserContext context, string? projectId);
    Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryCreateChangeProposalAsync(UserContext context, CreateProjectChangeProposalRequest request);
    Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryResolveChangeProposalAsync(UserContext context, string id, string action);
    CalendarMonthResponse GetCalendarMonth(UserContext context, DateOnly month);
    IReadOnlyList<UpcomingEventResponse> GetUpcomingEvents(UserContext context, int limit);
    IReadOnlyList<UpcomingEventResponse> GetPastEvents(UserContext context, int limit);
    Task<AiChatResponse> GetAiChatReply(UserContext context, AiChatRequest request, string apiKey);
}
