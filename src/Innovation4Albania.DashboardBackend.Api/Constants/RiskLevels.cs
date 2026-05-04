namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class RiskLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static readonly IReadOnlyList<string> All = [Low, Medium, High, Critical];

    public static string ToLabel(string risk) => risk switch
    {
        Low => "I ulët",
        Medium => "Mesatar",
        High => "I lartë",
        Critical => "Kritik",
        _ => risk
    };
}
