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
    public async Task GetProjects_ReturnsOnlyEconomyInnovationProjectsForDedicatedMinister()
    {
        var store = StoreTestHelpers.CreateStore();
        var ministry = ApplicationRoles.FixedMinistryForRole(ApplicationRoles.MinisterEkonomiseInovacionit)!;
        var context = UserContext.From(ApplicationRoles.MinisterEkonomiseInovacionit, null);

        var projects = await store.GetProjects(context, null, null);

        Assert.NotEmpty(projects);
        Assert.All(projects, project => Assert.Contains(ministry, project.Ministries));
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
