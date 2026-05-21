using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class AiChatLimits
{
    public const int MaxMessageLength = 4000;
    public const int MaxHistoryMessages = 20;
    public const int MaxHistoryMessageLength = 4000;

    public static bool TryValidate(AiChatRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            error = "Mesazhi nuk mund te jete bosh.";
            return false;
        }

        if (request.Message.Length > MaxMessageLength)
        {
            error = $"Mesazhi nuk mund te jete me i gjate se {MaxMessageLength} karaktere.";
            return false;
        }

        if (request.History is { Count: > MaxHistoryMessages })
        {
            error = $"Historia e bisedes nuk mund te kete me shume se {MaxHistoryMessages} mesazhe.";
            return false;
        }

        if (request.History?.Any(message => message.Content?.Length > MaxHistoryMessageLength) == true)
        {
            error = $"Cdo mesazh ne histori duhet te jete maksimumi {MaxHistoryMessageLength} karaktere.";
            return false;
        }

        error = null;
        return true;
    }
}
