namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class ApplicationRoles
{
    public const string Kryeminister = "kryeminister";
    public const string Minister = "minister";
    public const string Admin = "admin";
    public const string DrejtorAgjencie = "drejtor_agjencie";
    public const string DrejtorInovacioniPublik = "drejtor_inovacioni_publik";
    public const string StafAgjencie = "staf_agjencie";
    public const string Ekspert = "ekspert";
    public const string Specialist = "specialist";
    public const string StafMinistrie = "staf_ministrie";

    public static readonly IReadOnlyList<string> All =
    [
        Kryeminister,
        Minister,
        Admin,
        DrejtorAgjencie,
        DrejtorInovacioniPublik,
        StafAgjencie,
        Ekspert,
        Specialist,
        StafMinistrie
    ];

    public static bool IsAgencyContributor(string role) => role is StafAgjencie or Ekspert or Specialist;
    public static bool RequiresMinistry(string role) => role is Minister or StafMinistrie;
    public static bool CanCreateProjects(string role) => role is DrejtorAgjencie or DrejtorInovacioniPublik;
    public static bool CanManagePortfolio(string role) => role is DrejtorAgjencie or DrejtorInovacioniPublik;
    public static bool CanSubmitUpdates(string role) => (role is DrejtorAgjencie or DrejtorInovacioniPublik) || IsAgencyContributor(role);
    public static bool CanProposeProjectChanges(string role) => IsAgencyContributor(role) || role == StafMinistrie;
    public static bool CanDeleteChangeProposals(string role) => (role is DrejtorAgjencie or DrejtorInovacioniPublik) || IsAgencyContributor(role);
    public static bool CanViewRiskDeviations(string role) => (role is DrejtorAgjencie or DrejtorInovacioniPublik or StafMinistrie) || IsAgencyContributor(role);
    public static bool IsViewOnlyRole(string role) => role is Kryeminister or Minister;
    public static bool CanUseInteractiveLogin(string role) => (role is Admin or DrejtorAgjencie or DrejtorInovacioniPublik or StafMinistrie) || IsAgencyContributor(role);
    public static bool CanManageUsers(string role) => role == Admin;

    public static string ToDisplayLabel(string role) => role switch
    {
        Kryeminister => "Kryeministër",
        Minister => "Ministër",
        Admin => "Admin",
        DrejtorAgjencie => "Innovation4Albania",
        DrejtorInovacioniPublik => "Drejtor i Drejtorisë së Inovacionit Publik",
        StafAgjencie => "Ekspert Innovation4Albania",
        Ekspert => "Ekspert",
        Specialist => "Specialist",
        StafMinistrie => "Përfaqësues Ministrie",
        _ => role
    };
}
