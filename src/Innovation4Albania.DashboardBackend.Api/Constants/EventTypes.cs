namespace Innovation4Albania.DashboardBackend.Api.Constants;

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
