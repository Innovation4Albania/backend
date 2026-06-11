using Innovation4Albania.DashboardBackend.Api.Constants;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class ApplicationRolesTests
{
    [Theory]
    [InlineData(ApplicationRoles.StafMinistrie, true)]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, true)]
    [InlineData(ApplicationRoles.Minister, true)]
    [InlineData(ApplicationRoles.MinisterEkonomiseInovacionit, false)]
    [InlineData(ApplicationRoles.Kryeminister, false)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    public void RequiresMinistry_MinistryScopedViewRolesRequireMinistry(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.RequiresMinistry(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.Kryeminister, null)]
    [InlineData(ApplicationRoles.MinisterEkonomiseInovacionit, null)]
    public void FixedMinistryForRole_PortfolioRolesAreNotMinistryScoped(string role, string? expected)
    {
        Assert.Equal(expected, ApplicationRoles.FixedMinistryForRole(role));
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
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    public void CanProposeProjectChanges_SubmittingRolesCanPropose(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanProposeProjectChanges(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.Kryeminister, true)]
    [InlineData(ApplicationRoles.Minister, true)]
    [InlineData(ApplicationRoles.MinisterEkonomiseInovacionit, true)]
    [InlineData(ApplicationRoles.StafMinistrie, true)]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, true)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, true)]
    [InlineData(ApplicationRoles.StafAgjencie, true)]
    [InlineData(ApplicationRoles.Ekspert, true)]
    [InlineData(ApplicationRoles.Specialist, true)]
    [InlineData(ApplicationRoles.Admin, true)]
    public void IsViewOnlyRole_AllRolesCanOpenViewSessions(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.IsViewOnlyRole(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, false)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    [InlineData(ApplicationRoles.Ekspert, false)]
    [InlineData(ApplicationRoles.Specialist, false)]
    [InlineData(ApplicationRoles.Admin, false)]
    [InlineData(ApplicationRoles.Kryeminister, false)]
    [InlineData(ApplicationRoles.Minister, false)]
    [InlineData(ApplicationRoles.MinisterEkonomiseInovacionit, false)]
    [InlineData(ApplicationRoles.StafMinistrie, false)]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, false)]
    public void CanUseInteractiveLogin_CredentialsAreTemporarilyDisabled(string role, bool expected)
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

    [Theory]
    [InlineData(ApplicationRoles.Admin, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, true)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, true)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    [InlineData(ApplicationRoles.Ekspert, false)]
    [InlineData(ApplicationRoles.Specialist, false)]
    [InlineData(ApplicationRoles.StafMinistrie, false)]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, false)]
    [InlineData(ApplicationRoles.Minister, false)]
    public void CanReadManagedUsers_AdminAndProjectCreatorsCanReadManagedUsers(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanReadManagedUsers(role));
    }

    [Fact]
    public void ToDisplayLabel_PublicInnovationDirectorUsesFullDirectorateLabel()
    {
        Assert.Equal(
            "Drejtor i Inovacionit Publik",
            ApplicationRoles.ToDisplayLabel(ApplicationRoles.DrejtorInovacioniPublik));
    }

    [Theory]
    [InlineData(ApplicationRoles.DrejtorAgjencie, "Innovation4Albania")]
    [InlineData(ApplicationRoles.MinisterEkonomiseInovacionit, "Ministër i Ekonomisë dhe Inovacionit")]
    [InlineData(ApplicationRoles.Admin, "Admin")]
    [InlineData(ApplicationRoles.StafAgjencie, "Ekspert Innovation4Albania")]
    [InlineData(ApplicationRoles.Ekspert, "Ekspert Teknologjie")]
    [InlineData(ApplicationRoles.Specialist, "Specialist")]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, "PÃ«rfaqÃ«sues Institucioni")]
    [InlineData(ApplicationRoles.StafMinistrie, "Përfaqësues Ministrie")]
    public void ToDisplayLabel_UsesUpdatedAccessRoleLabels(string role, string expected)
    {
        Assert.Equal(expected, ApplicationRoles.ToDisplayLabel(role));
    }
}
