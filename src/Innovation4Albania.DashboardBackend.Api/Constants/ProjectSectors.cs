using System.Globalization;

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

    public static readonly IReadOnlyList<string> All =
    [
        Digitalization,
        Infrastructure,
        PublicServices,
        Governance,
        Education,
        Health,
        Agriculture,
        Environment
    ];

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
        _ => FormatCustomLabel(value)
    };

    private static string FormatCustomLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var culture = CultureInfo.GetCultureInfo("sq-AL");

        return string.Join(
            " ",
            value
                .Replace("_", " ", StringComparison.Ordinal)
                .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Length == 1
                    ? char.ToUpper(part[0], culture).ToString(culture)
                    : char.ToUpper(part[0], culture) + part[1..].ToLower(culture)));
    }
}
