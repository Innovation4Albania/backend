using Innovation4Albania.DashboardBackend.Api.Data;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreProjectMutationTests
{
    [Fact]
    public async Task TryCreateProjectAsync_RejectsDuplicateCode()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with { Code = "UNIQUE-001" };

        var created = await store.TryCreateProjectAsync(context, request);
        var duplicateCreated = await store.TryCreateProjectAsync(context, request with { Name = "Projekt tjeter" });

        Assert.True(created.IsSuccess);
        Assert.Null(created.Error);
        Assert.False(duplicateCreated.IsSuccess);
        Assert.Contains("kod", duplicateCreated.Error);
    }

    [Fact]
    public async Task TryUpdateProjectAsync_RejectsDuplicateCodeFromAnotherProject()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var firstCreated = await store.TryCreateProjectAsync(context, StoreTestHelpers.ValidProjectRequest() with { Code = "UNIQUE-101" });
        var secondRequest = StoreTestHelpers.ValidProjectRequest() with { Code = "UNIQUE-102", Name = "Projekt i dyte" };
        var secondCreated = await store.TryCreateProjectAsync(context, secondRequest);

        var updated = await store.TryUpdateProjectAsync(context, secondCreated.Response!.Id, secondRequest with { Code = firstCreated.Response!.Code });

        Assert.True(firstCreated.IsSuccess);
        Assert.True(secondCreated.IsSuccess);
        Assert.False(updated.IsSuccess);
        Assert.Contains("kod", updated.Error);
    }

    [Fact]
    public async Task TryUpdateProjectAsync_AllowsKeepingOwnCode()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with { Code = "UNIQUE-201" };

        var created = await store.TryCreateProjectAsync(context, request);
        var updated = await store.TryUpdateProjectAsync(context, created.Response!.Id, request with { Name = "Projekt i perditesuar" });

        Assert.True(created.IsSuccess);
        Assert.True(updated.IsSuccess);
        Assert.Null(updated.Error);
        Assert.Equal("UNIQUE-201", updated.Response!.Code);
        Assert.Equal("Projekt i perditesuar", updated.Response.Name);
    }

    [Fact]
    public async Task TryCreateProjectAsync_WaitsForSnapshotPersistence()
    {
        var persistence = new BlockingPersistence();
        var store = StoreTestHelpers.CreateStore(persistence);
        var context = StoreTestHelpers.DirectorContext();

        var createTask = store.TryCreateProjectAsync(context, StoreTestHelpers.ValidProjectRequest() with { Code = "AWAIT-001" });
        await persistence.SaveStarted.Task;
        await Task.Delay(25);

        Assert.False(createTask.IsCompleted);

        persistence.AllowSave.SetResult();
        var result = await createTask;

        Assert.True(result.IsSuccess);
        Assert.True(persistence.SaveCompleted);
    }

    private sealed class BlockingPersistence : IDashboardStorePersistence
    {
        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool SaveCompleted { get; private set; }
        public bool IsConfigured => true;

        public Task<string?> LoadSnapshotAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

        public async Task SaveSnapshotAsync(string payload, CancellationToken cancellationToken = default)
        {
            SaveStarted.SetResult();
            await AllowSave.Task.WaitAsync(cancellationToken);
            SaveCompleted = true;
        }
    }
}
