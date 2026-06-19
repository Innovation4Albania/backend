using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummary(UserContext context);
    Task<IReadOnlyList<StatusDistributionItem>> GetStatusDistribution(UserContext context);
    Task<IReadOnlyList<PerformanceScoreItem>> GetPerformance(UserContext context);
    Task<IReadOnlyList<TrendPointResponse>> GetTrend(UserContext context, int months);
    Task<IReadOnlyList<MinistryDistributionItem>> GetMinistryDistribution(UserContext context);
    Task<ResourceCapacitySummaryResponse> GetResourceCapacity(UserContext context);
    Task<IReadOnlyList<PerformanceBoardColumnResponse>> GetPerformanceBoard(UserContext context);
    Task<IReadOnlyList<RiskDeviationResponse>> GetRiskDeviations(UserContext context);
    Task<IReadOnlyList<ExpertPortfolioExpertResponse>> GetExpertPortfolioExperts(UserContext context);
    Task<ExpertPortfolioResponse?> GetExpertPortfolio(UserContext context, string userId);
}
