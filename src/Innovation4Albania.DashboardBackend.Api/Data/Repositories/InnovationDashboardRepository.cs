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
    public bool TryCreateProject(UserContext context, CreateProjectRequest request, out ProjectResponse? response, out string? error) => store.TryCreateProject(context, request, out response, out error);
    public bool TryUpdateProject(UserContext context, string id, CreateProjectRequest request, out ProjectResponse? response, out string? error) => store.TryUpdateProject(context, id, request, out response, out error);
    public bool TryDeleteProject(UserContext context, string id, out string? error) => store.TryDeleteProject(context, id, out error);
    public IReadOnlyList<ProjectEventResponse> GetEventsForProject(string projectId, UserContext context) => store.GetEventsForProject(projectId, context);

    public Task<AiInsightResponse?> GetProjectAiInsights(string projectId, UserContext context, string apiKey)
        => store.GetProjectAiInsights(projectId, context, apiKey);

    public IReadOnlyList<PerformanceBoardColumnResponse> GetPerformanceBoard(UserContext context)
        => store.GetPerformanceBoard(context);

    public PortfolioOkrResponse GetPortfolioOkr(UserContext context) => store.GetPortfolioOkr(context);
    public bool TryCreatePortfolioObjective(UserContext context, CreatePortfolioObjectiveRequest request, out ObjectiveResponse? response, out string? error) => store.TryCreatePortfolioObjective(context, request, out response, out error);
    public bool TryUpdatePortfolioObjective(UserContext context, string id, CreatePortfolioObjectiveRequest request, out ObjectiveResponse? response, out string? error) => store.TryUpdatePortfolioObjective(context, id, request, out response, out error);
    public bool TryDeletePortfolioObjective(UserContext context, string id, out string? error) => store.TryDeletePortfolioObjective(context, id, out error);
    public IReadOnlyList<RiskDeviationResponse> GetRiskDeviations(UserContext context) => store.GetRiskDeviations(context);
    public IReadOnlyList<WeeklyUpdateResponse> GetWeeklyUpdates(UserContext context, string? projectId) => store.GetWeeklyUpdates(context, projectId);
    public bool TryCreateWeeklyUpdate(UserContext context, CreateWeeklyUpdateRequest request, out WeeklyUpdateResponse? response, out string? error) => store.TryCreateWeeklyUpdate(context, request, out response, out error);
    public IReadOnlyList<ProjectChangeProposalResponse> GetChangeProposals(UserContext context, string? projectId) => store.GetChangeProposals(context, projectId);
    public bool TryCreateChangeProposal(UserContext context, CreateProjectChangeProposalRequest request, out ProjectChangeProposalResponse? response, out string? error) => store.TryCreateChangeProposal(context, request, out response, out error);
    public bool TryResolveChangeProposal(UserContext context, string id, string action, out ProjectChangeProposalResponse? response, out string? error) => store.TryResolveChangeProposal(context, id, action, out response, out error);
    public CalendarMonthResponse GetCalendarMonth(UserContext context, DateOnly month) => store.GetCalendarMonth(context, month);
    public IReadOnlyList<UpcomingEventResponse> GetUpcomingEvents(UserContext context, int limit) => store.GetUpcomingEvents(context, limit);
    public Task<AiChatResponse> GetAiChatReply(UserContext context, AiChatRequest request, string apiKey)
        => store.GetAiChatReply(context, request, apiKey);
}
