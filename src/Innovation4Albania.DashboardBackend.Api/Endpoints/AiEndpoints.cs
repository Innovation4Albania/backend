using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class AiEndpoints
{
    private const int MaxAiChatMessageLength = 4000;
    private const int MaxAiChatHistoryMessages = 20;
    private const int MaxAiChatHistoryMessageLength = 4000;

    public static RouteGroupBuilder MapAiEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/ai/chat", async (ClaimsPrincipal user, AiChatRequest request,
            IUserContextService contextService, IAiService service, IConfiguration configuration) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            if (!TryValidateAiChatRequest(request, out var validationError))
            {
                return Results.BadRequest(new ApiErrorResponse("validation_error", validationError!));
            }

            var apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
            var result = await service.GetChatReply(context, request, apiKey);
            return Results.Ok(result);
        }).RequireRateLimiting("ai");

        return api;
    }

    private static bool TryValidateAiChatRequest(AiChatRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            error = "Mesazhi nuk mund te jete bosh.";
            return false;
        }

        if (request.Message.Length > MaxAiChatMessageLength)
        {
            error = $"Mesazhi nuk mund te jete me i gjate se {MaxAiChatMessageLength} karaktere.";
            return false;
        }

        if (request.History is { Count: > MaxAiChatHistoryMessages })
        {
            error = $"Historia e bisedes nuk mund te kete me shume se {MaxAiChatHistoryMessages} mesazhe.";
            return false;
        }

        if (request.History?.Any(message => message.Content.Length > MaxAiChatHistoryMessageLength) == true)
        {
            error = $"Cdo mesazh ne histori duhet te jete maksimumi {MaxAiChatHistoryMessageLength} karaktere.";
            return false;
        }

        error = null;
        return true;
    }
}
