using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IPortfolioService
{
    PortfolioOkrResponse GetPortfolioOkr(UserContext context);
    bool TryCreatePortfolioObjective(UserContext context, CreatePortfolioObjectiveRequest request, out ObjectiveResponse? response, out string? error);
    bool TryUpdatePortfolioObjective(UserContext context, string id, CreatePortfolioObjectiveRequest request, out ObjectiveResponse? response, out string? error);
    bool TryDeletePortfolioObjective(UserContext context, string id, out string? error);
}
