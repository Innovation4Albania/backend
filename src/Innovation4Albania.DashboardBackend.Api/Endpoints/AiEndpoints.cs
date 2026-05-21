using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class AiEndpoints
{
    public static RouteGroupBuilder MapAiEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/ai/chat", async (ClaimsPrincipal user, AiChatRequest request,
            IUserContextService contextService, IAiService service, IConfiguration configuration) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            var apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
            var result = await service.GetChatReply(context, request, apiKey);
            return Results.Ok(result);
        }).RequireRateLimiting("ai");

        return api;
    }
}
