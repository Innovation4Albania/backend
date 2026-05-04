using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IDashboardService
{
    DashboardSummaryResponse GetSummary(UserContext context);
    IReadOnlyList<StatusDistributionItem> GetStatusDistribution(UserContext context);
    IReadOnlyList<PerformanceScoreItem> GetPerformance(UserContext context);
    IReadOnlyList<TrendPointResponse> GetTrend(int months);
    IReadOnlyList<MinistryDistributionItem> GetMinistryDistribution(UserContext context);
    ResourceCapacitySummaryResponse GetResourceCapacity(UserContext context);
    IReadOnlyList<PerformanceBoardColumnResponse> GetPerformanceBoard(UserContext context);
    IReadOnlyList<RiskDeviationResponse> GetRiskDeviations(UserContext context);
}
