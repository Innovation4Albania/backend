using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Data.Repositories;

public sealed class InnovationDashboardRepository(InnovationDashboardStore store) : IInnovationDashboardRepository
{
    public IReadOnlyList<string> GetMinistries() => store.GetMinistries();
    public string? ValidateLogin(LoginRequest request) => store.ValidateLogin(request);
    public bool IsValidContext(UserContext context, out string? error) => store.IsValidContext(context, out error);
    public UserResponse Login(LoginRequest request) => store.Login(request);
    public DashboardSummaryResponse GetDashboardSummary(UserContext context) => store.GetDashboardSummary(context);
    public IReadOnlyList<StatusDistributionItem> GetStatusDistribution(UserContext context) => store.GetStatusDistribution(context);
    public IReadOnlyList<MinistryDistributionItem> GetMinistryDistribution(UserContext context) => store.GetMinistryDistribution(context);
    public ResourceCapacitySummaryResponse GetResourceCapacitySummary(UserContext context) => store.GetResourceCapacitySummary(context);
    public IReadOnlyList<PerformanceScoreItem> GetPerformanceScores(UserContext context) => store.GetPerformanceScores(context);
    public IReadOnlyList<TrendPointResponse> GetTrend(int months) => store.GetTrend(months);
    public IReadOnlyList<ProjectResponse> GetProjects(UserContext context, string? status, string? query) => store.GetProjects(context, status, query);
    public ProjectResponse? GetProjectById(string id, UserContext context) => store.GetProjectById(id, context);
    public Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryCreateProjectAsync(UserContext context, CreateProjectRequest request) => store.TryCreateProjectAsync(context, request);
    public Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryUpdateProjectAsync(UserContext context, string id, CreateProjectRequest request) => store.TryUpdateProjectAsync(context, id, request);
    public Task<(bool IsSuccess, string? Error)> TryDeleteProjectAsync(UserContext context, string id) => store.TryDeleteProjectAsync(context, id);
    public IReadOnlyList<ProjectEventResponse> GetEventsForProject(string projectId, UserContext context) => store.GetEventsForProject(projectId, context);

    public Task<AiInsightResponse?> GetProjectAiInsights(string projectId, UserContext context, string apiKey)
        => store.GetProjectAiInsights(projectId, context, apiKey);

    public IReadOnlyList<PerformanceBoardColumnResponse> GetPerformanceBoard(UserContext context)
        => store.GetPerformanceBoard(context);

    public PortfolioOkrResponse GetPortfolioOkr(UserContext context) => store.GetPortfolioOkr(context);
    public Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryCreatePortfolioObjectiveAsync(UserContext context, CreatePortfolioObjectiveRequest request) => store.TryCreatePortfolioObjectiveAsync(context, request);
    public Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryUpdatePortfolioObjectiveAsync(UserContext context, string id, CreatePortfolioObjectiveRequest request) => store.TryUpdatePortfolioObjectiveAsync(context, id, request);
    public Task<(bool IsSuccess, string? Error)> TryDeletePortfolioObjectiveAsync(UserContext context, string id) => store.TryDeletePortfolioObjectiveAsync(context, id);
    public IReadOnlyList<RiskDeviationResponse> GetRiskDeviations(UserContext context) => store.GetRiskDeviations(context);
    public IReadOnlyList<WeeklyUpdateResponse> GetWeeklyUpdates(UserContext context, string? projectId) => store.GetWeeklyUpdates(context, projectId);
    public Task<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)> TryCreateWeeklyUpdateAsync(UserContext context, CreateWeeklyUpdateRequest request) => store.TryCreateWeeklyUpdateAsync(context, request);
    public IReadOnlyList<ProjectChangeProposalResponse> GetChangeProposals(UserContext context, string? projectId) => store.GetChangeProposals(context, projectId);
    public Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryCreateChangeProposalAsync(UserContext context, CreateProjectChangeProposalRequest request) => store.TryCreateChangeProposalAsync(context, request);
    public Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryResolveChangeProposalAsync(UserContext context, string id, string action) => store.TryResolveChangeProposalAsync(context, id, action);
    public CalendarMonthResponse GetCalendarMonth(UserContext context, DateOnly month) => store.GetCalendarMonth(context, month);
    public IReadOnlyList<UpcomingEventResponse> GetUpcomingEvents(UserContext context, int limit) => store.GetUpcomingEvents(context, limit);
    public IReadOnlyList<UpcomingEventResponse> GetPastEvents(UserContext context, int limit) => store.GetPastEvents(context, limit);
    public Task<AiChatResponse> GetAiChatReply(UserContext context, AiChatRequest request, string apiKey)
        => store.GetAiChatReply(context, request, apiKey);
}
