using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using Innovation4Albania.DashboardBackend.Api.Constants;
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

            if (!AiChatLimits.TryValidate(request, out var validationError))
            {
                return Results.BadRequest(new ApiErrorResponse("validation_error", validationError!));
            }

            var apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
            var result = await service.GetChatReply(context, request, apiKey);
            return Results.Ok(result);
        }).RequireRateLimiting("ai");

        return api;
    }
}
