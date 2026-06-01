using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreChangeProposalTests
{
    [Fact]
    public async Task TryResolveChangeProposalAsync_RejectsNonManagerRole()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext(userId: "staff-a");
        var project = await CreateAssignedProject(store, "PROP-NON-MANAGER", staff);
        var proposal = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal(project.Response!.Id));
        Assert.True(proposal.IsSuccess);

        var resolved = await store.TryResolveChangeProposalAsync(staff, proposal.Response!.Id, "approve");

        Assert.False(resolved.IsSuccess);
        Assert.Null(resolved.Response);
        Assert.Contains("shqyrto", resolved.Error);
    }

    [Fact]
    public async Task TryResolveChangeProposalAsync_ApprovesContentProposalAndUpdatesProjectDescription()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext(userId: "staff-a");
        var director = StoreTestHelpers.DirectorContext();
        var nextDescription = "Pershkrimi i perditesuar nga propozimi.";
        var project = await CreateAssignedProject(store, "PROP-APPROVE", staff);
        var request = new CreateProjectChangeProposalRequest(project.Response!.Id, "content", "old", nextDescription, "Duhet perditesuar.");
        var proposal = await store.TryCreateChangeProposalAsync(staff, request);
        Assert.True(proposal.IsSuccess);

        var resolved = await store.TryResolveChangeProposalAsync(director, proposal.Response!.Id, "approve");
        var updatedProject = await store.GetProjectById(project.Response.Id, director);

        Assert.True(resolved.IsSuccess);
        Assert.Null(resolved.Error);
        Assert.Equal(ChangeProposalStatuses.Approved, resolved.Response!.Status);
        Assert.Equal(nextDescription, updatedProject!.Description);
    }

    [Fact]
    public async Task TryResolveChangeProposalAsync_SavesOptionalResolutionReason()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext(userId: "staff-a");
        var director = StoreTestHelpers.DirectorContext();
        var project = await CreateAssignedProject(store, "PROP-REASON", staff);
        var proposal = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal(project.Response!.Id));

        var resolved = await store.TryResolveChangeProposalAsync(director, proposal.Response!.Id, "reject", "Nuk ka buxhet ne kete faze.");
        var proposals = await store.GetChangeProposals(staff, project.Response.Id);

        Assert.True(resolved.IsSuccess);
        Assert.Equal(ChangeProposalStatuses.Rejected, resolved.Response!.Status);
        Assert.Equal("Nuk ka buxhet ne kete faze.", resolved.Response.ResolutionReason);
        Assert.Equal("Nuk ka buxhet ne kete faze.", proposals.Single().ResolutionReason);
    }

    [Fact]
    public async Task TryResolveChangeProposalAsync_RejectsUnknownAction()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext(userId: "staff-a");
        var director = StoreTestHelpers.DirectorContext();
        var project = await CreateAssignedProject(store, "PROP-UNKNOWN", staff);
        var proposal = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal(project.Response!.Id));
        Assert.True(proposal.IsSuccess);

        var resolved = await store.TryResolveChangeProposalAsync(director, proposal.Response!.Id, "hold");

        Assert.False(resolved.IsSuccess);
        Assert.Null(resolved.Response);
        Assert.Contains("approve ose reject", resolved.Error);
    }

    [Fact]
    public async Task TryCreateChangeProposalAsync_AllowsMinistryRepresentativeForVisibleProject()
    {
        var store = StoreTestHelpers.CreateStore();
        var representative = StoreTestHelpers.MinistryRepresentativeContext();

        var proposal = await store.TryCreateChangeProposalAsync(representative, ValidContentProposal("p2"));

        Assert.True(proposal.IsSuccess);
        Assert.Equal(ApplicationRoles.ToDisplayLabel(ApplicationRoles.StafMinistrie), proposal.Response!.SubmittedRole);
    }

    [Fact]
    public async Task TryCreateChangeProposalAsync_RejectsMinistryRepresentativeForOtherMinistryProject()
    {
        var store = StoreTestHelpers.CreateStore();
        var representative = StoreTestHelpers.MinistryRepresentativeContext();

        var proposal = await store.TryCreateChangeProposalAsync(representative, ValidContentProposal("p3"));

        Assert.False(proposal.IsSuccess);
        Assert.Contains("Projekti nuk u gjet", proposal.Error);
    }

    [Fact]
    public async Task TryCreateChangeProposalAsync_UsesNextHighestIdAfterProjectDelete()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext("staff-a", "Erblin Malkurti", "staff-a");
        var director = StoreTestHelpers.DirectorContext();
        var staffB = StoreTestHelpers.StaffContext("staff-b", "Eralda Alhysa", "staff-b");
        var staffC = StoreTestHelpers.StaffContext("staff-c", "Evjenia Gjici", "staff-c");
        var firstProject = await CreateAssignedProject(store, "PROP-ID-1", staff);
        var secondProject = await CreateAssignedProject(store, "PROP-ID-2", staffB);
        var thirdProject = await CreateAssignedProject(store, "PROP-ID-3", staffC);

        var first = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal(firstProject.Response!.Id));
        var second = await store.TryCreateChangeProposalAsync(staffB, ValidContentProposal(secondProject.Response!.Id));
        var third = await store.TryCreateChangeProposalAsync(staffC, ValidContentProposal(thirdProject.Response!.Id));
        var deleted = await store.TryDeleteProjectAsync(director, firstProject.Response.Id);
        var created = await store.TryCreateChangeProposalAsync(staffC, ValidContentProposal(thirdProject.Response.Id));
        var proposals = await store.GetChangeProposals(director, null);
        var proposalIds = proposals.Select(proposal => proposal.Id).ToList();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(third.IsSuccess);
        Assert.True(deleted.IsSuccess);
        Assert.True(created.IsSuccess);
        Assert.Equal("chg-4", created.Response!.Id);
        Assert.Equal(proposalIds.Count, proposalIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task TryDeleteChangeProposalAsync_AllowsStaffToDeleteOwnProposal()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext("staff-a", "Erblin Malkurti", "staff-a");
        var project = await CreateAssignedProject(store, "PROP-DELETE-OWN", staff);
        var proposal = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal(project.Response!.Id));

        var deleted = await store.TryDeleteChangeProposalAsync(staff, proposal.Response!.Id);

        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task TryDeleteChangeProposalAsync_RejectsStaffDeletingAnotherStaffProposal()
    {
        var store = StoreTestHelpers.CreateStore();
        var staffA = StoreTestHelpers.StaffContext("staff-a", "Erblin Malkurti", "staff-a");
        var staffB = StoreTestHelpers.StaffContext("staff-b", "Eralda Alhysa", "staff-b");
        var director = StoreTestHelpers.DirectorContext();
        var project = await CreateAssignedProject(store, "PROP-DELETE-OTHER", staffA, staffB);
        var proposal = await store.TryCreateChangeProposalAsync(staffA, ValidContentProposal(project.Response!.Id));

        var deleted = await store.TryDeleteChangeProposalAsync(staffB, proposal.Response!.Id);
        var proposals = await store.GetChangeProposals(director, null);

        Assert.False(deleted.IsSuccess);
        Assert.Contains("vetem propozimet", deleted.Error);
        Assert.Single(proposals);
    }

    private static CreateProjectChangeProposalRequest ValidContentProposal(string projectId = "p1") =>
        new(projectId, "content", "old", "Pershkrim i ri.", "Arsye test.");

    private static Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> CreateAssignedProject(
        InnovationDashboardStore store,
        string code,
        params UserContext[] staff) =>
        store.TryCreateProjectAsync(
            StoreTestHelpers.DirectorContext(),
            StoreTestHelpers.ValidProjectRequest() with
            {
                Code = code,
                TeamMembers = staff
                    .Select(member => new WorkgroupMemberInput(member.FullName!, WorkgroupRoles.InnovationExpert, "Agjenci", 100 / staff.Length, member.UserId))
                    .ToList()
            });
}
