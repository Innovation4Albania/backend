namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class ApplicationRoles
{
    public const string Kryeminister = "kryeminister";
    public const string Minister = "minister";
    public const string MinisterEkonomiseInovacionit = "minister_ekonomie_inovacioni";
    public const string Admin = "admin";
    public const string DrejtorAgjencie = "drejtor_agjencie";
    public const string DrejtorInovacioniPublik = "drejtor_inovacioni_publik";
    public const string DrejtorEkosistemiStartupeve = "drejtor_ekosistemi_startupeve";
    public const string DrejtorProgrametMbeshtetjes = "drejtor_programet_mbeshtetjes";
    public const string DrejtorFinancimiAlternativ = "drejtor_financimi_alternativ";
    public const string DrejtorProjekteBe = "drejtor_projekte_be";
    public const string StafAgjencie = "staf_agjencie";
    public const string Ekspert = "ekspert";
    public const string EkspertEkosistemiStartupeve = "ekspert_ekosistemi_startupeve";
    public const string EkspertProgrametMbeshtetjes = "ekspert_programet_mbeshtetjes";
    public const string EkspertFinancimiAlternativ = "ekspert_financimi_alternativ";
    public const string EkspertProjekteBe = "ekspert_projekte_be";
    public const string Specialist = "specialist";
    public const string StafMinistrie = "staf_ministrie";
    public const string PerfaqesuesInstitucioni = "perfaqesues_institucioni";

    public static readonly IReadOnlyList<string> All =
    [
        Kryeminister,
        Minister,
        MinisterEkonomiseInovacionit,
        Admin,
        DrejtorAgjencie,
        DrejtorInovacioniPublik,
        DrejtorEkosistemiStartupeve,
        DrejtorProgrametMbeshtetjes,
        DrejtorFinancimiAlternativ,
        DrejtorProjekteBe,
        StafAgjencie,
        Ekspert,
        EkspertEkosistemiStartupeve,
        EkspertProgrametMbeshtetjes,
        EkspertFinancimiAlternativ,
        EkspertProjekteBe,
        Specialist,
        StafMinistrie,
        PerfaqesuesInstitucioni
    ];

    public static readonly IReadOnlyList<string> ManagedUserRoles =
    [
        Kryeminister,
        Minister,
        MinisterEkonomiseInovacionit,
        Admin,
        DrejtorAgjencie,
        DrejtorInovacioniPublik,
        DrejtorEkosistemiStartupeve,
        DrejtorProgrametMbeshtetjes,
        DrejtorFinancimiAlternativ,
        DrejtorProjekteBe,
        StafAgjencie,
        Ekspert,
        EkspertEkosistemiStartupeve,
        EkspertProgrametMbeshtetjes,
        EkspertFinancimiAlternativ,
        EkspertProjekteBe,
        Specialist,
        StafMinistrie,
        PerfaqesuesInstitucioni
    ];

    public static bool IsAgencyContributor(string role) =>
        role is StafAgjencie
            or Ekspert
            or EkspertEkosistemiStartupeve
            or EkspertProgrametMbeshtetjes
            or EkspertFinancimiAlternativ
            or EkspertProjekteBe
            or Specialist;
    public static bool IsManagedUserRole(string role) => ManagedUserRoles.Contains(role);
    public static bool IsInnovationDirector(string role) =>
        role is DrejtorAgjencie
            or DrejtorInovacioniPublik
            or DrejtorEkosistemiStartupeve
            or DrejtorProgrametMbeshtetjes
            or DrejtorFinancimiAlternativ
            or DrejtorProjekteBe;
    public static bool IsScopedDirector(string role) => GetScopedExpertRole(role) is not null;
    public static string? GetScopedExpertRole(string role) => role switch
    {
        DrejtorEkosistemiStartupeve => EkspertEkosistemiStartupeve,
        DrejtorProgrametMbeshtetjes => EkspertProgrametMbeshtetjes,
        DrejtorFinancimiAlternativ => EkspertFinancimiAlternativ,
        DrejtorProjekteBe => EkspertProjekteBe,
        _ => null
    };
    public static IReadOnlyList<string> GetReadableManagedRoles(string role)
    {
        var scopedExpertRole = GetScopedExpertRole(role);
        return scopedExpertRole is null ? ManagedUserRoles : [scopedExpertRole];
    }
    public static string? FixedMinistryForRole(string role) => null;
    public static bool RequiresMinistry(string role) => role is Minister or StafMinistrie or PerfaqesuesInstitucioni;
    public static bool CanCreateProjects(string role) => IsInnovationDirector(role);
    public static bool CanManagePortfolio(string role) => IsInnovationDirector(role);
    public static bool CanSubmitUpdates(string role) => IsInnovationDirector(role) || IsAgencyContributor(role);
    public static bool CanProposeProjectChanges(string role) => IsAgencyContributor(role) || role is StafMinistrie or PerfaqesuesInstitucioni;
    public static bool CanDeleteChangeProposals(string role) => IsInnovationDirector(role) || IsAgencyContributor(role);
    public static bool CanViewRiskDeviations(string role) => IsInnovationDirector(role) || role is StafMinistrie or PerfaqesuesInstitucioni || IsAgencyContributor(role);
    public static bool IsViewOnlyRole(string role) => All.Contains(role);
    public static bool CanUseInteractiveLogin(string role) => false;
    public static bool CanManageUsers(string role) => role == Admin;
    public static bool CanReadManagedUsers(string role) => CanManageUsers(role) || CanCreateProjects(role);

    public static string ToDisplayLabel(string role) => role switch
    {
        Kryeminister => "Kryeministër",
        Minister => "Ministër",
        MinisterEkonomiseInovacionit => "Ministër i Ekonomisë dhe Inovacionit",
        Admin => "Admin",
        DrejtorAgjencie => "Innovation4Albania",
        DrejtorInovacioniPublik => "Drejtor i Inovacionit Publik",
        DrejtorEkosistemiStartupeve => "Drejtor për ekosistemin e Start-upeve",
        DrejtorProgrametMbeshtetjes => "Drejtor për programet e mbështetjes",
        DrejtorFinancimiAlternativ => "Drejtor për zhvillimin e financimit alternativ",
        DrejtorProjekteBe => "Drejtor për zhvillimin e projekteve me BE-në",
        StafAgjencie => "Ekspert Innovation4Albania",
        Ekspert => "Ekspert Teknologjie",
        EkspertEkosistemiStartupeve => "Ekspert për ekosistemin e Start-upeve",
        EkspertProgrametMbeshtetjes => "Ekspert për programet e mbështetjes",
        EkspertFinancimiAlternativ => "Ekspert për zhvillimin e financimit alternativ",
        EkspertProjekteBe => "Ekspert për zhvillimin e projekteve me BE-në",
        Specialist => "Specialist",
        PerfaqesuesInstitucioni => "Përfaqësues Institucioni",
        StafMinistrie => "Përfaqësues Ministrie",
        _ => role
    };
}
