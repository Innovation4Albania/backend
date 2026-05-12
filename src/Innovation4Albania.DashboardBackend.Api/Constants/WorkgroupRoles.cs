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
    public const string ProjectOfficer = "project_officer";
    public const string InnovationExpert = "innovation_expert";
    public const string Specialist = "specialist";

    public static readonly IReadOnlyList<string> All = [ProjectLead, OkrOwner, BusinessAnalyst, LegalExpert, TechnicalCoordinator, DataSpecialist, MinistryRepresentative, ProjectOfficer, InnovationExpert, Specialist];

    public static string ToLabel(string value) => value switch
    {
        ProjectLead => "Drejtues projekti",
        OkrOwner => "Pronar OKR",
        BusinessAnalyst => "Analist biznesi",
        LegalExpert => "Ekspert ligjor",
        TechnicalCoordinator => "Koordinator teknik",
        DataSpecialist => "Specialist të dhënash",
        MinistryRepresentative => "Përfaqësues ministrie",
        ProjectOfficer => "Ekspert",
        InnovationExpert => "Ekspert Inovacioni",
        Specialist => "Specialist",
        _ => value
    };
}
