using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class AiService(IInnovationDashboardRepository repository) : IAiService
{
    public Task<AiChatResponse> GetChatReply(UserContext context, AiChatRequest request) => repository.GetAiChatReply(context, request);
}
