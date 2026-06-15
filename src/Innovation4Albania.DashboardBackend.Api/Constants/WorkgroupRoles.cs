namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class WorkgroupRoles
{
    public const string ProjectLead = "project_lead";
    public const string OkrOwner = "okr_owner";
    public const string BusinessAnalyst = "business_analyst";
    public const string LegalExpert = "legal_expert";
    public const string TechnicalCoordinator = "technical_coordinator";
    public const string DataSpecialist = "data_specialist";
    public const string MinistryRepresentative = "ministry_representative";
    public const string SectorManager = "pergjegjes_sektori";
    public const string ProjectOfficer = "project_officer";
    public const string InnovationExpert = "innovation_expert";
    public const string StartupEcosystemExpert = "startup_ecosystem_expert";
    public const string SupportProgramsExpert = "support_programs_expert";
    public const string AlternativeFinancingExpert = "alternative_financing_expert";
    public const string EuProjectsExpert = "eu_projects_expert";
    public const string Specialist = "specialist";

    public static readonly IReadOnlyList<string> All = [ProjectLead, OkrOwner, BusinessAnalyst, LegalExpert, TechnicalCoordinator, DataSpecialist, MinistryRepresentative, SectorManager, ProjectOfficer, InnovationExpert, StartupEcosystemExpert, SupportProgramsExpert, AlternativeFinancingExpert, EuProjectsExpert, Specialist];

    public static string ToLabel(string value) => value switch
    {
        ProjectLead => "Drejtues projekti",
        OkrOwner => "Pronar OKR",
        BusinessAnalyst => "Analist biznesi",
        LegalExpert => "Ekspert ligjor",
        TechnicalCoordinator => "Koordinator teknik",
        DataSpecialist => "Specialist të dhënash",
        MinistryRepresentative => "Përfaqësues ministrie",
        SectorManager => "PÃ«rgjegjÃ«s Sektori",
        ProjectOfficer => "Ekspert",
        InnovationExpert => "Ekspert për inovacionin publik",
        StartupEcosystemExpert => "Ekspert për ekosistemin e Start-upeve",
        SupportProgramsExpert => "Ekspert për programet e mbështetjes",
        AlternativeFinancingExpert => "Ekspert për zhvillimin e financimit alternativ",
        EuProjectsExpert => "Ekspert për zhvillimin e projekteve me BE-në",
        Specialist => "Specialist",
        _ => value
    };
}
