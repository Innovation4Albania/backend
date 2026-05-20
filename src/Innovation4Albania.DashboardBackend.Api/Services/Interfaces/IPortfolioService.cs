using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioOkrResponse> GetPortfolioOkr(UserContext context);
    Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryCreatePortfolioObjectiveAsync(UserContext context, CreatePortfolioObjectiveRequest request);
    Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryUpdatePortfolioObjectiveAsync(UserContext context, string id, CreatePortfolioObjectiveRequest request);
    Task<(bool IsSuccess, string? Error)> TryDeletePortfolioObjectiveAsync(UserContext context, string id);
}
