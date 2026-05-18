namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreProjectMutationTests
{
    [Fact]
    public void TryCreateProject_RejectsDuplicateCode()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with { Code = "UNIQUE-001" };

        var created = store.TryCreateProject(context, request, out _, out var createError);
        var duplicateCreated = store.TryCreateProject(context, request with { Name = "Projekt tjeter" }, out _, out var duplicateError);

        Assert.True(created);
        Assert.Null(createError);
        Assert.False(duplicateCreated);
        Assert.Contains("kod", duplicateError);
    }

    [Fact]
    public void TryUpdateProject_RejectsDuplicateCodeFromAnotherProject()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var firstCreated = store.TryCreateProject(context, StoreTestHelpers.ValidProjectRequest() with { Code = "UNIQUE-101" }, out var first, out _);
        var secondRequest = StoreTestHelpers.ValidProjectRequest() with { Code = "UNIQUE-102", Name = "Projekt i dyte" };
        var secondCreated = store.TryCreateProject(context, secondRequest, out var second, out _);

        var updated = store.TryUpdateProject(context, second!.Id, secondRequest with { Code = first!.Code }, out _, out var error);

        Assert.True(firstCreated);
        Assert.True(secondCreated);
        Assert.False(updated);
        Assert.Contains("kod", error);
    }

    [Fact]
    public void TryUpdateProject_AllowsKeepingOwnCode()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with { Code = "UNIQUE-201" };

        var created = store.TryCreateProject(context, request, out var project, out _);
        var updated = store.TryUpdateProject(context, project!.Id, request with { Name = "Projekt i perditesuar" }, out var updatedProject, out var error);

        Assert.True(created);
        Assert.True(updated);
        Assert.Null(error);
        Assert.Equal("UNIQUE-201", updatedProject!.Code);
        Assert.Equal("Projekt i perditesuar", updatedProject.Name);
    }
}
