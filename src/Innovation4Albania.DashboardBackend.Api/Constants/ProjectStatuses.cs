namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class ProjectStatuses
{
    public const string Planning = "planning";
    public const string Active = "active";
    public const string AtRisk = "at_risk";
    public const string Blocked = "blocked";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Planning,
        Active,
        AtRisk,
        Blocked,
        Completed,
        Cancelled
    ];

    public static string ToLabel(string value) => value switch
    {
        Planning => "Planifikim",
        Active => "Aktive",
        AtRisk => "Në risk",
        Blocked => "Pauzë",
        Completed => "Përfunduara",
        Cancelled => "Të anuluara",
        _ => value
    };

    public static string ToColor(string value) => value switch
    {
        Planning => "hsl(var(--warning))",
        Active => "hsl(var(--destructive))",
        AtRisk => "hsl(var(--destructive))",
        Blocked => "hsl(var(--muted-foreground))",
        Completed => "hsl(var(--success))",
        Cancelled => "hsl(var(--muted))",
        _ => "hsl(var(--muted-foreground))"
    };
}
