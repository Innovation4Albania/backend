using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class PortfolioService(IInnovationDashboardRepository repository) : IPortfolioService
{
    public PortfolioOkrResponse GetPortfolioOkr(UserContext context) => repository.GetPortfolioOkr(context);
    public bool TryCreatePortfolioObjective(UserContext context, CreatePortfolioObjectiveRequest request, out ObjectiveResponse? response, out string? error) => repository.TryCreatePortfolioObjective(context, request, out response, out error);
}
