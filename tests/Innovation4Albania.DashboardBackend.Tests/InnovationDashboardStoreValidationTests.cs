using System.Reflection;
using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreValidationTests
{
    [Fact]
    public void TryValidateProjectRequest_AcceptsValidRequest()
    {
        var (isValid, error) = InvokeTryValidateProjectRequest(StoreTestHelpers.ValidProjectRequest());

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsMissingName()
    {
        var request = StoreTestHelpers.ValidProjectRequest() with { Name = "" };

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("Kodi dhe emri", error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsMissingDescription()
    {
        var request = StoreTestHelpers.ValidProjectRequest() with { Description = " " };

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("Përshkrimi", error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsMissingMinistries()
    {
        var request = StoreTestHelpers.ValidProjectRequest() with { Ministries = [] };

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("ministri", error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsLeadWithNonLetters()
    {
        var request = StoreTestHelpers.ValidProjectRequest() with { Lead = "Drejtor 1" };

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("Përgjegjësi", error);
    }

    [Fact]
    public void TryValidateProjectRequest_AllowsMissingLead()
    {
        var request = StoreTestHelpers.ValidProjectRequest() with { Lead = "" };

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsInvalidStatus()
    {
        var request = StoreTestHelpers.ValidProjectRequest(status: "unknown");

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("Statusi", error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsEndDateBeforeStartDate()
    {
        var start = DateTimeOffset.UtcNow.AddDays(5);
        var end = DateTimeOffset.UtcNow.AddDays(1);
        var request = StoreTestHelpers.ValidProjectRequest(start, end);

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("Data e mbylljes", error);
    }

    [Fact]
    public void IsValidContext_RejectsMinistryStaffWithoutMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = UserContext.From(ApplicationRoles.StafMinistrie, null);

        var isValid = store.IsValidContext(context, out var error);

        Assert.False(isValid);
        Assert.Contains("ministrie", error);
    }

    [Fact]
    public void IsValidContext_AcceptsMinistryStaffWithKnownMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var ministry = store.GetMinistries()[0];
        var context = UserContext.From(ApplicationRoles.StafMinistrie, ministry);

        var isValid = store.IsValidContext(context, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public async Task GetProjects_ReturnsNoProjectsForMinistryStaffWithoutMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = UserContext.From(ApplicationRoles.StafMinistrie, null);

        var projects = await store.GetProjects(context, null, null);

        Assert.Empty(projects);
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyAccountMinistryProjectsForMinistryStaff()
    {
        var store = StoreTestHelpers.CreateStore();
        var ministry = "Ministria e Financave";
        var context = UserContext.From(ApplicationRoles.StafMinistrie, ministry, "finance.rep", "Përfaqësues Financash", "rep-1");

        var projects = await store.GetProjects(context, null, null);

        Assert.NotEmpty(projects);
        Assert.All(projects, project => Assert.Contains(ministry, project.Ministries));
    }

    [Fact]
    public void IsValidContext_RejectsMinisterWithoutMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = UserContext.From(ApplicationRoles.Minister, null);

        var isValid = store.IsValidContext(context, out var error);

        Assert.False(isValid);
        Assert.Contains("ministrie", error);
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlySelectedMinistryProjectsForMinister()
    {
        var store = StoreTestHelpers.CreateStore();
        var ministry = "Ministria e Financave";
        var context = UserContext.From(ApplicationRoles.Minister, ministry);

        var projects = await store.GetProjects(context, null, null);

        Assert.NotEmpty(projects);
        Assert.All(projects, project => Assert.Contains(ministry, project.Ministries));
    }

    [Fact]
    public async Task GetProjects_ReturnsAllProjectsForEconomyInnovationMinister()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = UserContext.From(ApplicationRoles.MinisterEkonomiseInovacionit, null);
        var primeMinisterProjects = await store.GetProjects(UserContext.From(ApplicationRoles.Kryeminister, null), null, null);

        var projects = await store.GetProjects(context, null, null);

        Assert.NotEmpty(projects);
        Assert.Equal(primeMinisterProjects.Count, projects.Count);
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyScopedProjectsForInstitutionRepresentative()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = "AKSHI-001",
            Agency = "AKSHI"
        };
        var created = await store.TryCreateProjectAsync(director, request);
        var context = UserContext.From(ApplicationRoles.PerfaqesuesInstitucioni, "AKSHI");

        var projects = await store.GetProjects(context, null, null);

        Assert.Single(projects);
        Assert.Equal(created.Response!.Id, projects[0].Id);
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyWorkgroupProjectsForAgencyExpert()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = "ASSIGNED-001",
            TeamMembers = [new WorkgroupMemberInput("Ekspert Test", WorkgroupRoles.ProjectOfficer, "Agjenci", 100, "expert-1")]
        };
        var created = await store.TryCreateProjectAsync(director, request);
        var context = StoreTestHelpers.StaffContext("expert.test", "Ekspert Test", "expert-1");

        var projects = await store.GetProjects(context, null, null);

        Assert.Single(projects);
        Assert.Equal(created.Response!.Id, projects[0].Id);
        Assert.Contains(projects[0].TeamMembers, member => member.UserId == "expert-1");
    }

    [Theory]
    [InlineData(ApplicationRoles.StafAgjencie, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.Ekspert, WorkgroupRoles.ProjectOfficer)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.EkspertProjekteBe, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.Specialist, WorkgroupRoles.Specialist)]
    public async Task GetProjects_ReturnsOnlyAssignedProjectsForContributorAccounts(string role, string workgroupRole)
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var assigned = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = $"ASSIGNED-{role}",
            TeamMembers = [new WorkgroupMemberInput("Contributor Test", workgroupRole, "Njësi test", 100, "contributor-1")]
        };
        var unassigned = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = $"UNASSIGNED-{role}",
            TeamMembers = [new WorkgroupMemberInput("Other Contributor", workgroupRole, "Njësi test", 100, "contributor-2")]
        };
        var createdAssigned = await store.TryCreateProjectAsync(director, assigned);
        await store.TryCreateProjectAsync(director, unassigned);
        var context = UserContext.From(role, null, "contributor.test", "Contributor Test", "contributor-1");

        var projects = await store.GetProjects(context, null, null);

        Assert.Single(projects);
        Assert.Equal(createdAssigned.Response!.Id, projects[0].Id);
        Assert.All(projects, project => Assert.Contains(project.TeamMembers, member => member.UserId == "contributor-1"));
    }

    [Theory]
    [InlineData(ApplicationRoles.StafAgjencie, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.Ekspert, WorkgroupRoles.ProjectOfficer)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.EkspertProjekteBe, WorkgroupRoles.InnovationExpert)]
    [InlineData(ApplicationRoles.Specialist, WorkgroupRoles.Specialist)]
    public async Task GetProjects_DoesNotUseFullNameFallbackForContributorAccounts(string role, string workgroupRole)
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var fullName = "Identical Contributor";
        var current = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = $"CURRENT-{role}",
            TeamMembers = [new WorkgroupMemberInput(fullName, workgroupRole, "Njësi test", 100, "contributor-1")]
        };
        var sameNameDifferentUser = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = $"SAME-NAME-{role}",
            TeamMembers = [new WorkgroupMemberInput(fullName, workgroupRole, "Njësi test", 100, "contributor-2")]
        };
        var historicalWithoutUserId = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = $"HISTORICAL-{role}",
            TeamMembers = [new WorkgroupMemberInput(fullName, workgroupRole, "Njësi test", 100)]
        };
        var createdCurrent = await store.TryCreateProjectAsync(director, current);
        await store.TryCreateProjectAsync(director, sameNameDifferentUser);
        await store.TryCreateProjectAsync(director, historicalWithoutUserId);
        var context = UserContext.From(role, null, "contributor.test", fullName, "contributor-1");

        var projects = await store.GetProjects(context, null, null);

        var project = Assert.Single(projects);
        Assert.Equal(createdCurrent.Response!.Id, project.Id);
        Assert.All(projects, item => Assert.Contains(item.TeamMembers, member => member.UserId == "contributor-1"));
    }

    [Fact]
    public async Task GetProjects_ScopedDirectorSeesOnlyProjectsWithMatchingExpertRole()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var startupProject = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = "STARTUP-DIRECTOR",
            TeamMembers =
            [
                new WorkgroupMemberInput(
                    "Startup Expert",
                    WorkgroupRoles.InnovationExpert,
                    "Drejtoria e Inovacionit",
                    100,
                    "startup-expert-1",
                    ApplicationRoles.EkspertEkosistemiStartupeve)
            ]
        };
        var fundingProject = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = "FUNDING-DIRECTOR",
            TeamMembers =
            [
                new WorkgroupMemberInput(
                    "Funding Expert",
                    WorkgroupRoles.InnovationExpert,
                    "Drejtoria e Inovacionit",
                    100,
                    "funding-expert-1",
                    ApplicationRoles.EkspertFinancimiAlternativ)
            ]
        };
        var supportProject = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = "SUPPORT-DIRECTOR",
            TeamMembers =
            [
                new WorkgroupMemberInput(
                    "Support Expert",
                    WorkgroupRoles.InnovationExpert,
                    "Drejtoria e Inovacionit",
                    100,
                    "support-expert-1",
                    ApplicationRoles.EkspertProgrametMbeshtetjes)
            ]
        };
        var createdStartup = await store.TryCreateProjectAsync(director, startupProject);
        var createdSupport = await store.TryCreateProjectAsync(director, supportProject);
        await store.TryCreateProjectAsync(director, fundingProject);
        var context = UserContext.From(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve, null);

        var projects = await store.GetProjects(context, null, null);

        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, project => project.Id == createdStartup.Response!.Id);
        Assert.Contains(projects, project => project.Id == createdSupport.Response!.Id);
        Assert.DoesNotContain(projects, project => project.Code == "FUNDING-DIRECTOR");
    }

    [Theory]
    [InlineData(
        ApplicationRoles.DrejtorInovacioniPublik,
        ApplicationRoles.StafAgjencie,
        "DREJTORIA E INOVACIONIT PER ADMINISTRATEN PUBLIKE",
        "PUBLIC-INNOVATION")]
    [InlineData(
        ApplicationRoles.DrejtorTeDhenaTeknologjiPlatforma,
        ApplicationRoles.Ekspert,
        "DREJTORIA PER TE DHENA, TEKNOLOGJI DHE PLATFORMA",
        "TECH-DIRECTOR")]
    public async Task GetProjects_ScopedDirectorSeesOnlyProjectsWithOwnDirectorateExperts(
        string directorRole,
        string expertRole,
        string visibleUnit,
        string visibleCode)
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var visibleProject = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = visibleCode,
            TeamMembers =
            [
                new WorkgroupMemberInput(
                    "Scoped Expert",
                    WorkgroupRoles.InnovationExpert,
                    visibleUnit,
                    100,
                    "scoped-expert-1",
                    expertRole)
            ]
        };
        var otherProject = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = $"{visibleCode}-OTHER",
            TeamMembers =
            [
                new WorkgroupMemberInput(
                    "Other Expert",
                    WorkgroupRoles.InnovationExpert,
                    "DREJTORIA E FINANCIMIT ALTERNATIV DHE NDERKOMBETARIZIMIT",
                    100,
                    "other-expert-1",
                    expertRole)
            ]
        };
        var createdVisible = await store.TryCreateProjectAsync(director, visibleProject);
        await store.TryCreateProjectAsync(director, otherProject);
        var context = UserContext.From(directorRole, null);

        var projects = await store.GetProjects(context, null, null);

        var project = Assert.Single(projects);
        Assert.Equal(createdVisible.Response!.Id, project.Id);
        Assert.Equal(visibleCode, project.Code);
    }

    [Fact]
    public async Task GetProjectById_HidesProjectOutsideAgencyExpertsWorkgroup()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with
        {
            Code = "ASSIGNED-002",
            TeamMembers = [new WorkgroupMemberInput("Emër i Njëjtë", WorkgroupRoles.ProjectOfficer, "Agjenci", 100, "expert-1")]
        };
        var created = await store.TryCreateProjectAsync(director, request);
        var context = StoreTestHelpers.StaffContext("expert.other", "Emër i Njëjtë", "expert-2");

        var project = await store.GetProjectById(created.Response!.Id, context);

        Assert.Null(project);
    }

    [Fact]
    public void Login_CanonicalizesMinistryWithReplacementCharacters()
    {
        var store = StoreTestHelpers.CreateStore();
        var brokenMinistry = "Ministria e Infrastruktur\uFFFDs dhe Energjis\uFFFD";
        var request = new LoginRequest(ApplicationRoles.StafMinistrie, brokenMinistry, "Staf Ministrie");

        var isValid = store.IsValidContext(UserContext.From(request.Role, request.Ministry), out var error);
        var user = store.Login(request);

        Assert.True(isValid);
        Assert.Null(error);
        Assert.Equal("Ministria e Infrastrukturës dhe Energjisë", user.Ministry);
    }

    private static (bool IsValid, string? Error) InvokeTryValidateProjectRequest(CreateProjectRequest request)
    {
        var method = typeof(InnovationDashboardStore).GetMethod("TryValidateProjectRequest", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(InnovationDashboardStore), "TryValidateProjectRequest");
        object?[] args = [request, null];

        var result = (bool)method.Invoke(null, args)!;

        return (result, args[1] as string);
    }
}
