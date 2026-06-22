using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class DashboardService(IInnovationDashboardRepository repository) : IDashboardService
{
    public Task<DashboardSummaryResponse> GetSummary(UserContext context) => repository.GetDashboardSummary(context);
    public Task<IReadOnlyList<StatusDistributionItem>> GetStatusDistribution(UserContext context) => repository.GetStatusDistribution(context);
    public Task<IReadOnlyList<PerformanceScoreItem>> GetPerformance(UserContext context) => repository.GetPerformanceScores(context);
    public Task<IReadOnlyList<TrendPointResponse>> GetTrend(UserContext context, int months) => repository.GetTrend(context, months);
    public Task<ProgramMetricsResponse?> GetProgramMetrics(string programKey) => repository.GetProgramMetrics(programKey);
    public Task<(bool IsSuccess, ProgramMetricsResponse? Response, string? Error)> UpdateProgramMetrics(UserContext context, string programKey, UpdateProgramMetricsRequest request) => repository.TryUpdateProgramMetricsAsync(context, programKey, request);
    public Task<IReadOnlyList<MinistryDistributionItem>> GetMinistryDistribution(UserContext context) => repository.GetMinistryDistribution(context);
    public Task<ResourceCapacitySummaryResponse> GetResourceCapacity(UserContext context) => repository.GetResourceCapacitySummary(context);
    public Task<IReadOnlyList<PerformanceBoardColumnResponse>> GetPerformanceBoard(UserContext context) => repository.GetPerformanceBoard(context);
    public Task<IReadOnlyList<RiskDeviationResponse>> GetRiskDeviations(UserContext context) => repository.GetRiskDeviations(context);
    public Task<IReadOnlyList<ExpertPortfolioExpertResponse>> GetExpertPortfolioExperts(UserContext context) => repository.GetExpertPortfolioExperts(context);
    public Task<ExpertPortfolioResponse?> GetExpertPortfolio(UserContext context, string userId) => repository.GetExpertPortfolio(context, userId);
}
