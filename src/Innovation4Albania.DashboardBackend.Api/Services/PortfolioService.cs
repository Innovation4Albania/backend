using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class PortfolioService(IInnovationDashboardRepository repository) : IPortfolioService
{
    public PortfolioOkrResponse GetPortfolioOkr(UserContext context) => repository.GetPortfolioOkr(context);
    public Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryCreatePortfolioObjectiveAsync(UserContext context, CreatePortfolioObjectiveRequest request) => repository.TryCreatePortfolioObjectiveAsync(context, request);
    public Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryUpdatePortfolioObjectiveAsync(UserContext context, string id, CreatePortfolioObjectiveRequest request) => repository.TryUpdatePortfolioObjectiveAsync(context, id, request);
    public Task<(bool IsSuccess, string? Error)> TryDeletePortfolioObjectiveAsync(UserContext context, string id) => repository.TryDeletePortfolioObjectiveAsync(context, id);
}
