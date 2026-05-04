using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class AiEndpoints
{
    public static RouteGroupBuilder MapAiEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/ai/chat", (string role, string? ministry, AiChatRequest request, IUserContextService contextService, IAiService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetChatReply(context, request))
                : errorResult!;
        });

        return api;
    }
}
