namespace Innovation4Albania.DashboardBackend.Api.Models;

public static class ApplicationRoles
{
    public const string Kryeminister = "kryeminister";
    public const string Minister = "minister";
    public const string DrejtorAgjencie = "drejtor_agjencie";
    public const string DrejtorInovacioniPublik = "drejtor_inovacioni_publik";
    public const string StafAgjencie = "staf_agjencie";
    public const string StafMinistrie = "staf_ministrie";

    public static readonly IReadOnlyList<string> All =
    [
        Kryeminister,
        Minister,
        DrejtorAgjencie,
        DrejtorInovacioniPublik,
        StafAgjencie,
        StafMinistrie
    ];

    public static bool RequiresMinistry(string role) => role == StafMinistrie;

    public static bool CanCreateProjects(string role) => role is DrejtorAgjencie or DrejtorInovacioniPublik;

    public static bool CanManagePortfolio(string role) => role is DrejtorAgjencie or DrejtorInovacioniPublik;

    public static bool CanSubmitUpdates(string role) => role is DrejtorAgjencie or DrejtorInovacioniPublik or StafAgjencie;

    public static bool CanProposeProjectChanges(string role) => role == StafAgjencie;

    public static string ToDisplayLabel(string role) => role switch
    {
        Kryeminister => "Kryeministër",
        Minister => "Ministër",
        DrejtorAgjencie => "Drejtori i Inovacionit",
        DrejtorInovacioniPublik => "Drejtor i Drejtorisë së Inovacionit Publik",
        StafAgjencie => "Ekspert Agjencie",
        StafMinistrie => "Staf Ministrie",
        _ => role
    };
}

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

public static class RiskLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static string ToLabel(string risk) => risk switch
    {
        Low => "I ulët",
        Medium => "Mesatar",
        High => "I lartë",
        Critical => "Kritik",
        _ => risk
    };
}

public static class PerformanceBuckets
{
    public const string Excellent = "excellent";
    public const string Good = "good";
    public const string NeedsAttention = "needs_attention";
    public const string Critical = "critical";

    public static string ToLabel(string bucket) => bucket switch
    {
        Excellent => "Shkëlqyeshëm",
        Good => "Mirë",
        NeedsAttention => "Kërkon vëmendje",
        Critical => "Kritik",
        _ => bucket
    };
}

public static class EventTypes
{
    public const string Kickoff = "kickoff";
    public const string Completion = "completion";

    public static string ToLabel(string value) => value switch
    {
        Kickoff => "Nisja e projektit",
        Completion => "Mbyllja e projektit",
        _ => value
    };
}
