namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class ChangeProposalStatuses
{
    public const string Pending = "Në shqyrtim";
    public const string Approved = "Miratuar";
    public const string Rejected = "Refuzuar";

    public static string Normalize(string status) =>
        status.Trim() switch
        {
            "Ne shqyrtim" => Pending,
            Pending => Pending,
            Approved => Approved,
            Rejected => Rejected,
            var value => value
        };
}
