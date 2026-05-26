using Innovation4Albania.DashboardBackend.Api.Constants;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class ApplicationRolesTests
{
    [Theory]
    [InlineData(ApplicationRoles.StafMinistrie, true)]
    [InlineData(ApplicationRoles.Minister, true)]
    [InlineData(ApplicationRoles.Kryeminister, false)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    public void RequiresMinistry_MinistryScopedViewRolesRequireMinistry(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.RequiresMinistry(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.DrejtorAgjencie, true)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, true)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    [InlineData(ApplicationRoles.Minister, false)]
    public void CanCreateProjects_OnlyDirectorRolesCanCreate(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanCreateProjects(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.StafAgjencie, true)]
    [InlineData(ApplicationRoles.StafMinistrie, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    public void CanProposeProjectChanges_SubmittingRolesCanPropose(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanProposeProjectChanges(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.Kryeminister, true)]
    [InlineData(ApplicationRoles.Minister, true)]
    [InlineData(ApplicationRoles.StafMinistrie, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    public void IsViewOnlyRole_OnlyExecutiveAndMinistryViewerRolesUseViewLinks(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.IsViewOnlyRole(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.DrejtorAgjencie, true)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, true)]
    [InlineData(ApplicationRoles.StafAgjencie, true)]
    [InlineData(ApplicationRoles.Kryeminister, false)]
    [InlineData(ApplicationRoles.StafMinistrie, false)]
    public void CanUseInteractiveLogin_OnlyAgencyRolesCanLogin(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanUseInteractiveLogin(role));
    }

    [Fact]
    public void ToDisplayLabel_PublicInnovationDirectorUsesFullDirectorateLabel()
    {
        Assert.Equal(
            "Drejtor i Drejtorisë së Inovacionit Publik",
            ApplicationRoles.ToDisplayLabel(ApplicationRoles.DrejtorInovacioniPublik));
    }

    [Theory]
    [InlineData(ApplicationRoles.DrejtorAgjencie, "Innovation4Albania")]
    [InlineData(ApplicationRoles.StafAgjencie, "Ekspert Innovation4Albania")]
    [InlineData(ApplicationRoles.StafMinistrie, "Përfaqësues Ministrie")]
    public void ToDisplayLabel_UsesUpdatedAccessRoleLabels(string role, string expected)
    {
        Assert.Equal(expected, ApplicationRoles.ToDisplayLabel(role));
    }
}
