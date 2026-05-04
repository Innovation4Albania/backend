namespace Innovation4Albania.DashboardBackend.Api.Constants;

public static class ProjectSectors
{
    public const string Digitalization = "digitalization";
    public const string Infrastructure = "infrastructure";
    public const string PublicServices = "public_services";
    public const string Governance = "governance";
    public const string Education = "education";
    public const string Health = "health";
    public const string Agriculture = "agriculture";
    public const string Environment = "environment";

    public static readonly IReadOnlyList<string> All = [Digitalization, Infrastructure, PublicServices, Governance, Education, Health, Agriculture, Environment];

    public static string ToLabel(string value) => value switch
    {
        Digitalization => "Digjitalizim",
        Infrastructure => "Infrastrukturë",
        PublicServices => "Shërbime publike",
        Governance => "Qeverisje",
        Education => "Arsim",
        Health => "Shëndetësi",
        Agriculture => "Bujqësi",
        Environment => "Mjedis",
        _ => value
    };
}
