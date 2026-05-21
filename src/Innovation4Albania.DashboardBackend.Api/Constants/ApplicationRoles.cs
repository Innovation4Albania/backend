namespace Innovation4Albania.DashboardBackend.Api.Constants;

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
    public static bool CanViewRiskDeviations(string role) => role is DrejtorAgjencie or DrejtorInovacioniPublik or StafAgjencie or StafMinistrie;
    public static bool IsViewOnlyRole(string role) => role is Kryeminister or Minister or StafMinistrie;
    public static bool CanUseInteractiveLogin(string role) => role is DrejtorAgjencie or DrejtorInovacioniPublik or StafAgjencie;

    public static string ToDisplayLabel(string role) => role switch
    {
        Kryeminister => "Kryeministër",
        Minister => "Ministër",
        DrejtorAgjencie => "Drejtor Agjencie",
        DrejtorInovacioniPublik => "Drejtor i Inovacionit Publik",
        StafAgjencie => "Ekspert Agjencie",
        StafMinistrie => "Staf Ministrie",
        _ => role
    };
}
