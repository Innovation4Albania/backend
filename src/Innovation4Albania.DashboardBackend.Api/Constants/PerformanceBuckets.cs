namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class PerformanceBuckets
{
    public const string Excellent = "excellent";
    public const string Good = "good";
    public const string NeedsAttention = "needs_attention";
    public const string Completed = "completed";

    public static string ToLabel(string bucket) => bucket switch
    {
        Excellent => "Në nivelin e duhur",
        Good => "Mirë",
        NeedsAttention => "Kërkon vëmendje",
        Completed => "Përfunduara",
        _ => bucket
    };
}
