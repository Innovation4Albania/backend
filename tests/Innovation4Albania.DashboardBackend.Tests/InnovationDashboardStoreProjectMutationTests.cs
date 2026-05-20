using System.Collections;
using System.Reflection;
using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Models;

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
    public async Task TryUpdateProjectAsync_RecalculatesStoredOkr()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var created = await store.TryCreateProjectAsync(context, StoreTestHelpers.ValidProjectRequest() with { Code = "OKR-UPDATE-001" });
        var project = GetProjectState(store, created.Response!.Id);

        var updated = await store.TryUpdateProjectAsync(
            context,
            created.Response.Id,
            StoreTestHelpers.ValidProjectRequest() with { Code = "OKR-UPDATE-001", Progress = 95 });
        var storedOkr = GetProperty<ProjectOkr>(project, "Okr");

        Assert.True(updated.IsSuccess);
        Assert.Equal(updated.Response!.Okr, storedOkr);
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

    [Fact]
    public async Task TryCreateProjectAsync_UsesNextHighestProjectIdAfterDelete()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var deleted = await store.TryDeleteProjectAsync(context, "p3");
        var created = await store.TryCreateProjectAsync(context, StoreTestHelpers.ValidProjectRequest() with { Code = "AFTER-DELETE-001" });
        var projectIds = (await store.GetProjects(context, null, null)).Select(project => project.Id).ToList();

        Assert.True(deleted.IsSuccess);
        Assert.True(created.IsSuccess);
        Assert.Equal("p8", created.Response!.Id);
        Assert.Equal(projectIds.Count, projectIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task TryCreateProjectAsync_AssignsUniqueIdsForConcurrentCreates()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var results = await Task.WhenAll(Enumerable.Range(1, 20)
            .Select(index => Task.Run(() => store.TryCreateProjectAsync(
                context,
                StoreTestHelpers.ValidProjectRequest() with { Code = $"CONCURRENT-{index:000}", Name = $"Projekt paralel {index}" }))));
        var ids = results.Select(result => result.Response!.Id).ToList();

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task TryCreateWeeklyUpdateAsync_AssignsUniqueIdsForConcurrentUpdates()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var results = await Task.WhenAll(Enumerable.Range(1, 20)
            .Select(index => Task.Run(() => store.TryCreateWeeklyUpdateAsync(
                context,
                new CreateWeeklyUpdateRequest("p1", $"Ekspert {index}", 40 + index, ProjectStatuses.Active, RiskLevels.Medium, "", $"Koment {index}")))));
        var ids = results.Select(result => result.Response!.Id).ToList();

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task TryCreateWeeklyUpdateAsync_RecalculatesStoredOkr()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var project = GetProjectState(store, "p1");

        var result = await store.TryCreateWeeklyUpdateAsync(
            context,
            new CreateWeeklyUpdateRequest("p1", "Ekspert OKR", 95, ProjectStatuses.Active, RiskLevels.Low, "", "Progres i ri"));
        var storedOkr = GetProperty<ProjectOkr>(project, "Okr");
        var projectResponse = await store.GetProjectById("p1", context);

        Assert.True(result.IsSuccess);
        Assert.Equal(projectResponse!.Okr, storedOkr);
        Assert.Equal(projectResponse.OkrAverage, result.Response!.OkrAverage);
    }

    [Fact]
    public async Task Store_AllowsConcurrentReadsDuringMutations()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var writeTasks = Enumerable.Range(1, 40)
            .Select(index => Task.Run(() => store.TryCreateWeeklyUpdateAsync(
                context,
                new CreateWeeklyUpdateRequest(
                    "p1",
                    $"Ekspert {index}",
                    35 + index,
                    ProjectStatuses.Active,
                    RiskLevels.Medium,
                    "",
                    $"Koment paralel {index}"))));

        var readTasks = Enumerable.Range(1, 40)
            .Select(_ => Task.Run(async () =>
            {
                await store.GetProjects(context, null, null);
                await store.GetWeeklyUpdates(context, "p1");
                await store.GetChangeProposals(context, null);
                await store.GetPortfolioOkr(context);
            }));

        var results = await Task.WhenAll(writeTasks);
        await Task.WhenAll(readTasks);

        Assert.All(results, result => Assert.True(result.IsSuccess));
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

    private static object GetProjectState(InnovationDashboardStore store, string projectId)
    {
        var field = typeof(InnovationDashboardStore).GetField("_projects", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingFieldException(nameof(InnovationDashboardStore), "_projects");
        var projects = (IEnumerable)field.GetValue(store)!;

        return projects.Cast<object>().First(project =>
            string.Equals(GetProperty<string>(project, "Id"), projectId, StringComparison.OrdinalIgnoreCase));
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMemberException(instance.GetType().Name, propertyName);
        return (T)property.GetValue(instance)!;
    }
}
