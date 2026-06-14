namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class ApplicationRoles
{
    public const string Kryeminister = "kryeminister";
    public const string Minister = "minister";
    public const string MinisterEkonomiseInovacionit = "minister_ekonomie_inovacioni";
    public const string Admin = "admin";
    public const string DrejtorAgjencie = "drejtor_agjencie";
    public const string DrejtorInovacioniPublik = "drejtor_inovacioni_publik";
    public const string DrejtorEkosistemiStartupeveLehtesuesve = "drejtor_ekosistemi_startupeve_lehtesuesve";
    public const string DrejtorFinancimiAlternativNderkombetarizimit = "drejtor_financimi_alternativ_nderkombetarizimit";
    public const string DrejtorTeDhenaTeknologjiPlatforma = "drejtor_te_dhena_teknologji_platforma";
    public const string PergjegjesSektori = "pergjegjes_sektori";
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
        DrejtorEkosistemiStartupeveLehtesuesve,
        DrejtorFinancimiAlternativNderkombetarizimit,
        DrejtorTeDhenaTeknologjiPlatforma,
        PergjegjesSektori,
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
        DrejtorEkosistemiStartupeveLehtesuesve,
        DrejtorFinancimiAlternativNderkombetarizimit,
        DrejtorTeDhenaTeknologjiPlatforma,
        PergjegjesSektori,
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
            or DrejtorEkosistemiStartupeveLehtesuesve
            or DrejtorFinancimiAlternativNderkombetarizimit
            or PergjegjesSektori;
    public static bool IsScopedDirector(string role) => GetScopedExpertRoles(role) is not null;
    public static IReadOnlyList<string>? GetScopedExpertRoles(string role) => role switch
    {
        DrejtorEkosistemiStartupeveLehtesuesve => [EkspertEkosistemiStartupeve, EkspertProgrametMbeshtetjes],
        DrejtorFinancimiAlternativNderkombetarizimit => [EkspertFinancimiAlternativ, EkspertProjekteBe],
        _ => null
    };
    public static IReadOnlyList<string> GetReadableManagedRoles(string role)
    {
        var scopedExpertRoles = GetScopedExpertRoles(role);
        return role is PergjegjesSektori ? [Specialist] : scopedExpertRoles is null ? ManagedUserRoles : scopedExpertRoles;
    }
    public static string? FixedMinistryForRole(string role) => null;
    public static bool RequiresMinistry(string role) => role is Minister or StafMinistrie or PerfaqesuesInstitucioni;
    public static bool AllowsManagedUnit(string role) => RequiresMinistry(role) || role is Specialist or PergjegjesSektori;
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
        DrejtorEkosistemiStartupeveLehtesuesve => "Drejtor i Ekosistemit të Start-up-eve dhe Lehtësuesve",
        DrejtorFinancimiAlternativNderkombetarizimit => "Drejtor i Financimit Alternativ dhe Ndërkombëtarizimit",
        DrejtorTeDhenaTeknologjiPlatforma => "Drejtor për të Dhëna, Teknologji dhe Platforma",
        PergjegjesSektori => "Përgjegjës Sektori",
        StafAgjencie => "Ekspert për inovacionin publik",
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
