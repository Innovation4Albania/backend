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
    [InlineData(ApplicationRoles.Ekspert, true)]
    [InlineData(ApplicationRoles.Specialist, true)]
    [InlineData(ApplicationRoles.StafMinistrie, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    public void CanProposeProjectChanges_SubmittingRolesCanPropose(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanProposeProjectChanges(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.Kryeminister, true)]
    [InlineData(ApplicationRoles.Minister, true)]
    [InlineData(ApplicationRoles.StafMinistrie, false)]
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
    [InlineData(ApplicationRoles.Ekspert, true)]
    [InlineData(ApplicationRoles.Specialist, true)]
    [InlineData(ApplicationRoles.Admin, true)]
    [InlineData(ApplicationRoles.Kryeminister, false)]
    [InlineData(ApplicationRoles.StafMinistrie, true)]
    public void CanUseInteractiveLogin_CredentialRolesCanLogin(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanUseInteractiveLogin(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.Admin, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, false)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    [InlineData(ApplicationRoles.Ekspert, false)]
    [InlineData(ApplicationRoles.Specialist, false)]
    public void CanManageUsers_OnlyAdminCanManageUsers(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanManageUsers(role));
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
    [InlineData(ApplicationRoles.Admin, "Admin")]
    [InlineData(ApplicationRoles.StafAgjencie, "Ekspert Innovation4Albania")]
    [InlineData(ApplicationRoles.Ekspert, "Ekspert")]
    [InlineData(ApplicationRoles.Specialist, "Specialist")]
    [InlineData(ApplicationRoles.StafMinistrie, "Përfaqësues Ministrie")]
    public void ToDisplayLabel_UsesUpdatedAccessRoleLabels(string role, string expected)
    {
        Assert.Equal(expected, ApplicationRoles.ToDisplayLabel(role));
    }
}
