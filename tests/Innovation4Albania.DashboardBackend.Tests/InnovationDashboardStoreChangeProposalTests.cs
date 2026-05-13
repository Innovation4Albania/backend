using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreChangeProposalTests
{
    [Fact]
    public void TryResolveChangeProposal_RejectsNonManagerRole()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext();
        Assert.True(store.TryCreateChangeProposal(staff, ValidContentProposal(), out var proposal, out _));

        var resolved = store.TryResolveChangeProposal(staff, proposal!.Id, "approve", out var response, out var error);

        Assert.False(resolved);
        Assert.Null(response);
        Assert.Contains("shqyrto", error);
    }

    [Fact]
    public void TryResolveChangeProposal_ApprovesContentProposalAndUpdatesProjectDescription()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext();
        var director = StoreTestHelpers.DirectorContext();
        var nextDescription = "Pershkrimi i perditesuar nga propozimi.";
        var request = new CreateProjectChangeProposalRequest("p1", "content", "old", nextDescription, "Duhet perditesuar.");
        Assert.True(store.TryCreateChangeProposal(staff, request, out var proposal, out _));

        var resolved = store.TryResolveChangeProposal(director, proposal!.Id, "approve", out var response, out var error);
        var project = store.GetProjectById("p1", director);

        Assert.True(resolved);
        Assert.Null(error);
        Assert.Equal("Miratuar", response!.Status);
        Assert.Equal(nextDescription, project!.Description);
    }

    [Fact]
    public void TryResolveChangeProposal_RejectsUnknownAction()
    {
        var store = StoreTestHelpers.CreateStore();
        var staff = StoreTestHelpers.StaffContext();
        var director = StoreTestHelpers.DirectorContext();
        Assert.True(store.TryCreateChangeProposal(staff, ValidContentProposal(), out var proposal, out _));

        var resolved = store.TryResolveChangeProposal(director, proposal!.Id, "hold", out var response, out var error);

        Assert.False(resolved);
        Assert.Null(response);
        Assert.Contains("approve ose reject", error);
    }

    private static CreateProjectChangeProposalRequest ValidContentProposal() =>
        new("p1", "content", "old", "Pershkrim i ri.", "Arsye test.");
}
