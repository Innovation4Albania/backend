using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IAiService
{
    Task<AiChatResponse> GetChatReply(UserContext context, AiChatRequest request);
}
