namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class ProjectPriorities
{
    public const string Critical = "critical";
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";

    public static readonly IReadOnlyList<string> All = [Critical, High, Medium, Low];

    public static string ToLabel(string value) => value switch
    {
        Critical => "Kritike",
        High => "E lartë",
        Medium => "Mesatar",
        Low => "Progresiv",
        _ => value
    };
}
