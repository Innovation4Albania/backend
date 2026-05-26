using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreChangeProposalTests
{
    [Fact]
    public async Task TryResolveChangeProposalAsync_RejectsNonManagerRole()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext();
        var proposal = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal());
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
        var staff = StoreTestHelpers.StaffContext();
        var director = StoreTestHelpers.DirectorContext();
        var nextDescription = "Pershkrimi i perditesuar nga propozimi.";
        var request = new CreateProjectChangeProposalRequest("p1", "content", "old", nextDescription, "Duhet perditesuar.");
        var proposal = await store.TryCreateChangeProposalAsync(staff, request);
        Assert.True(proposal.IsSuccess);

        var resolved = await store.TryResolveChangeProposalAsync(director, proposal.Response!.Id, "approve");
        var project = await store.GetProjectById("p1", director);

        Assert.True(resolved.IsSuccess);
        Assert.Null(resolved.Error);
        Assert.Equal(ChangeProposalStatuses.Approved, resolved.Response!.Status);
        Assert.Equal(nextDescription, project!.Description);
    }

    [Fact]
    public async Task TryResolveChangeProposalAsync_RejectsUnknownAction()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext();
        var director = StoreTestHelpers.DirectorContext();
        var proposal = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal());
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
        var staff = StoreTestHelpers.StaffContext();
        var director = StoreTestHelpers.DirectorContext();

        var first = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal("p1"));
        var second = await store.TryCreateChangeProposalAsync(StoreTestHelpers.StaffContext(fullName: "Eralda Alhysa"), ValidContentProposal("p2"));
        var third = await store.TryCreateChangeProposalAsync(StoreTestHelpers.StaffContext(fullName: "Evjenia Gjici"), ValidContentProposal("p3"));
        var deleted = await store.TryDeleteProjectAsync(director, "p1");
        var created = await store.TryCreateChangeProposalAsync(StoreTestHelpers.StaffContext(fullName: "Evjenia Gjici"), ValidContentProposal("p3"));
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
        var staff = StoreTestHelpers.StaffContext("staff-a");
        var proposal = await store.TryCreateChangeProposalAsync(staff, ValidContentProposal());

        var deleted = await store.TryDeleteChangeProposalAsync(staff, proposal.Response!.Id);

        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task TryDeleteChangeProposalAsync_RejectsStaffDeletingAnotherStaffProposal()
    {
        var store = StoreTestHelpers.CreateStore();
        var staffA = StoreTestHelpers.StaffContext("staff-a");
        var staffB = StoreTestHelpers.StaffContext("staff-b");
        var director = StoreTestHelpers.DirectorContext();
        var proposal = await store.TryCreateChangeProposalAsync(staffA, ValidContentProposal());

        var deleted = await store.TryDeleteChangeProposalAsync(staffB, proposal.Response!.Id);
        var proposals = await store.GetChangeProposals(director, null);

        Assert.False(deleted.IsSuccess);
        Assert.Contains("vetem propozimet", deleted.Error);
        Assert.Single(proposals);
    }

    private static CreateProjectChangeProposalRequest ValidContentProposal(string projectId = "p1") =>
        new(projectId, "content", "old", "Pershkrim i ri.", "Arsye test.");
}
