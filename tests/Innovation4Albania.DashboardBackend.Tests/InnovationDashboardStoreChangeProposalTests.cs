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
        Assert.Equal("Miratuar", resolved.Response!.Status);
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

    private static CreateProjectChangeProposalRequest ValidContentProposal() =>
        new("p1", "content", "old", "Pershkrim i ri.", "Arsye test.");
}
