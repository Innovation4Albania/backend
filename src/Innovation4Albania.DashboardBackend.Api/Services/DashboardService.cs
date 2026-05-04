using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class DashboardService(IInnovationDashboardRepository repository) : IDashboardService
{
    public DashboardSummaryResponse GetSummary(UserContext context) => repository.GetDashboardSummary(context);
    public IReadOnlyList<StatusDistributionItem> GetStatusDistribution(UserContext context) => repository.GetStatusDistribution(context);
    public IReadOnlyList<PerformanceScoreItem> GetPerformance(UserContext context) => repository.GetPerformanceScores(context);
    public IReadOnlyList<TrendPointResponse> GetTrend(int months) => repository.GetTrend(months);
    public IReadOnlyList<MinistryDistributionItem> GetMinistryDistribution(UserContext context) => repository.GetMinistryDistribution(context);
    public ResourceCapacitySummaryResponse GetResourceCapacity(UserContext context) => repository.GetResourceCapacitySummary(context);
    public IReadOnlyList<PerformanceBoardColumnResponse> GetPerformanceBoard(UserContext context) => repository.GetPerformanceBoard(context);
    public IReadOnlyList<RiskDeviationResponse> GetRiskDeviations(UserContext context) => repository.GetRiskDeviations(context);
}
