using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummary(UserContext context);
    Task<IReadOnlyList<StatusDistributionItem>> GetStatusDistribution(UserContext context);
    Task<IReadOnlyList<PerformanceScoreItem>> GetPerformance(UserContext context);
    IReadOnlyList<TrendPointResponse> GetTrend(int months);
    Task<IReadOnlyList<MinistryDistributionItem>> GetMinistryDistribution(UserContext context);
    Task<ResourceCapacitySummaryResponse> GetResourceCapacity(UserContext context);
    Task<IReadOnlyList<PerformanceBoardColumnResponse>> GetPerformanceBoard(UserContext context);
    Task<IReadOnlyList<RiskDeviationResponse>> GetRiskDeviations(UserContext context);
}
