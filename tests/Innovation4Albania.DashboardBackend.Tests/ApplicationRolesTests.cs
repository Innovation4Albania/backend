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
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, false)]
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, false)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit, false)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    [InlineData(ApplicationRoles.Minister, false)]
    public void CanCreateProjects_OnlyGeneralInnovationDirectorCanCreate(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanCreateProjects(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.StafAgjencie, true)]
    [InlineData(ApplicationRoles.Ekspert, true)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, true)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, true)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, true)]
    [InlineData(ApplicationRoles.EkspertProjekteBe, true)]
    [InlineData(ApplicationRoles.Specialist, true)]
    [InlineData(ApplicationRoles.StafMinistrie, true)]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, false)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit, false)]
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
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, true)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit, true)]
    [InlineData(ApplicationRoles.StafAgjencie, true)]
    [InlineData(ApplicationRoles.Ekspert, true)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, true)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, true)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, true)]
    [InlineData(ApplicationRoles.EkspertProjekteBe, true)]
    [InlineData(ApplicationRoles.Specialist, true)]
    [InlineData(ApplicationRoles.Admin, true)]
    public void IsViewOnlyRole_AllRolesCanOpenViewSessions(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.IsViewOnlyRole(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, false)]
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, false)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit, false)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    [InlineData(ApplicationRoles.Ekspert, false)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, false)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, false)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, false)]
    [InlineData(ApplicationRoles.EkspertProjekteBe, false)]
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
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, false)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit, false)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    [InlineData(ApplicationRoles.Ekspert, false)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, false)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, false)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, false)]
    [InlineData(ApplicationRoles.EkspertProjekteBe, false)]
    [InlineData(ApplicationRoles.Specialist, false)]
    public void CanManageUsers_OnlyAdminCanManageUsers(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanManageUsers(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.Admin, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, true)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, true)]
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, true)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit, true)]
    [InlineData(ApplicationRoles.StafAgjencie, false)]
    [InlineData(ApplicationRoles.Ekspert, false)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, false)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, false)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, false)]
    [InlineData(ApplicationRoles.EkspertProjekteBe, false)]
    [InlineData(ApplicationRoles.Specialist, false)]
    [InlineData(ApplicationRoles.StafMinistrie, false)]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, false)]
    [InlineData(ApplicationRoles.Minister, false)]
    public void CanReadManagedUsers_AdminAndProjectCreatorsCanReadManagedUsers(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanReadManagedUsers(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.StafAgjencie, true)]
    [InlineData(ApplicationRoles.Ekspert, true)]
    [InlineData(ApplicationRoles.Specialist, true)]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    [InlineData(ApplicationRoles.Admin, false)]
    public void CanManageProgramMetrics_OnlyAgencyContributorsCanManageManualProgramMetrics(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.CanManageProgramMetrics(role));
    }

    [Theory]
    [InlineData(ApplicationRoles.DrejtorAgjencie, false)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik, true)]
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, true)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit, true)]
    [InlineData(ApplicationRoles.DrejtorTeDhenaTeknologjiPlatforma, true)]
    [InlineData(ApplicationRoles.DrejtorEkonomiseSherbimeveMbeshtetese, true)]
    [InlineData(ApplicationRoles.StafAgjencie, true)]
    [InlineData(ApplicationRoles.Ekspert, true)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, true)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, true)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, true)]
    [InlineData(ApplicationRoles.EkspertProjekteBe, true)]
    [InlineData(ApplicationRoles.Specialist, true)]
    [InlineData(ApplicationRoles.PergjegjesSektori, true)]
    public void AllowsManagedUnit_PreservesDirectorateForScopedAgencyAccounts(string role, bool expected)
    {
        Assert.Equal(expected, ApplicationRoles.AllowsManagedUnit(role));
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
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, "Drejtor i Ekosistemit të Start-up-eve dhe Lehtësuesve")]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit, "Drejtor i Financimit Alternativ dhe Ndërkombëtarizimit")]
    [InlineData(ApplicationRoles.StafAgjencie, "Ekspert për inovacionin publik")]
    [InlineData(ApplicationRoles.Ekspert, "Ekspert Teknologjie")]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, "Ekspert për ekosistemin e Start-upeve")]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, "Ekspert për programet e mbështetjes")]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, "Ekspert për zhvillimin e financimit alternativ")]
    [InlineData(ApplicationRoles.EkspertProjekteBe, "Ekspert për zhvillimin e projekteve me BE-në")]
    [InlineData(ApplicationRoles.Specialist, "Specialist")]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni, "Përfaqësues Institucioni")]
    [InlineData(ApplicationRoles.StafMinistrie, "Përfaqësues Ministrie")]
    public void ToDisplayLabel_UsesUpdatedAccessRoleLabels(string role, string expected)
    {
        Assert.Equal(expected, ApplicationRoles.ToDisplayLabel(role));
    }
}
