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
    public async Task TryCreateProjectAsync_PreservesCustomWorkgroupRole()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        const string customRole = "Koordinator IA";

        var created = await store.TryCreateProjectAsync(
            context,
            StoreTestHelpers.ValidProjectRequest() with
            {
                Code = "CUSTOM-ROLE-001",
                TeamMembers =
                [
                    new WorkgroupMemberInput("Anetar Custom", customRole, "Njesi IA", 80)
                ]
            });

        Assert.True(created.IsSuccess);
        var member = Assert.Single(created.Response!.TeamMembers);
        Assert.Equal(customRole, member.Role);
        Assert.Equal(customRole, member.RoleLabel);
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
    public async Task TryCreateProjectAsync_DefaultsTotalPhasesAndStartsAtFirstPhase()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var created = await store.TryCreateProjectAsync(
            context,
            StoreTestHelpers.ValidProjectRequest() with { Code = "PHASE-CREATE-001", TotalPhases = 0, CurrentPhase = 5, Progress = 85 });

        Assert.True(created.IsSuccess);
        Assert.Equal(6, created.Response!.TotalPhases);
        Assert.Equal(1, created.Response.CurrentPhase);
    }

    [Fact]
    public async Task TryUpdateProjectAsync_RecalculatesCurrentPhaseFromProgress()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with { Code = "PHASE-UPDATE-001", TotalPhases = 6, Progress = 0 };
        var created = await store.TryCreateProjectAsync(context, request);

        var updated = await store.TryUpdateProjectAsync(
            context,
            created.Response!.Id,
            request with { Progress = 68, CurrentPhase = 1 });

        Assert.True(updated.IsSuccess);
        Assert.Equal(5, updated.Response!.CurrentPhase);
    }

    [Fact]
    public async Task TryCreateProjectAsync_CompletesProjectAtFullProgress()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var created = await store.TryCreateProjectAsync(
            context,
            StoreTestHelpers.ValidProjectRequest() with { Code = "COMPLETE-CREATE-001", Status = ProjectStatuses.Active, Progress = 100 });

        Assert.True(created.IsSuccess);
        Assert.Equal(ProjectStatuses.Completed, created.Response!.Status);
    }

    [Fact]
    public async Task TryUpdateProjectAsync_CompletesProjectAtFullProgress()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var request = StoreTestHelpers.ValidProjectRequest() with { Code = "COMPLETE-UPDATE-001", Status = ProjectStatuses.Active, Progress = 50 };
        var created = await store.TryCreateProjectAsync(context, request);

        var updated = await store.TryUpdateProjectAsync(
            context,
            created.Response!.Id,
            request with { Progress = 100 });

        Assert.True(updated.IsSuccess);
        Assert.Equal(ProjectStatuses.Completed, updated.Response!.Status);
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
    public async Task TryCreatePortfolioObjectiveAsync_UsesNextHighestIdAfterDelete()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var deleted = await store.TryDeletePortfolioObjectiveAsync(context, "portfolio-1");
        var created = await store.TryCreatePortfolioObjectiveAsync(
            context,
            new CreatePortfolioObjectiveRequest(
                "Objektiv i ri",
                "Drejtoria e Inovacionit",
                [new KeyResultInput("KR i ri", 10, 100, "%")]));
        var portfolio = await store.GetPortfolioOkr(context);
        var objectiveIds = portfolio.Objectives.Select(objective => objective.Id).ToList();

        Assert.True(deleted.IsSuccess);
        Assert.True(created.IsSuccess);
        Assert.Equal("portfolio-3", created.Response!.Id);
        Assert.Equal(objectiveIds.Count, objectiveIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
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
    public async Task TryCreateWeeklyUpdateAsync_UsesNextHighestIdAfterProjectDelete()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var deleted = await store.TryDeleteProjectAsync(context, "p1");
        var result = await store.TryCreateWeeklyUpdateAsync(
            context,
            new CreateWeeklyUpdateRequest("p3", "Ekspert pas fshirjes", 45, ProjectStatuses.Active, RiskLevels.Medium, "", "Koment"));
        var updates = await store.GetWeeklyUpdates(context, null);
        var updateIds = updates.Select(update => update.Id).ToList();

        Assert.True(deleted.IsSuccess);
        Assert.True(result.IsSuccess);
        Assert.Equal("upd-4", result.Response!.Id);
        Assert.Equal(updateIds.Count, updateIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
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
    public async Task GetTrend_UsesHistoricalUpdatesAndVisibleProjects()
    {
        var store = StoreTestHelpers.CreateStore();
        ClearProjectsAndUpdates(store);
        var director = StoreTestHelpers.DirectorContext();
        var financeContext = StoreTestHelpers.MinistryRepresentativeContext("Ministria e Financave");
        var startDate = DateTimeOffset.UtcNow.AddMonths(-4);
        var endDate = DateTimeOffset.UtcNow.AddMonths(4);
        var firstMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-2);
        var secondMonth = firstMonth.AddMonths(1);
        var currentMonth = firstMonth.AddMonths(2);

        var financeProject = await store.TryCreateProjectAsync(
            director,
            StoreTestHelpers.ValidProjectRequest(startDate, endDate) with
            {
                Code = "TREND-FIN",
                Name = "Projekt Finance",
                Ministries = ["Ministria e Financave"],
                Progress = 10,
            });
        var justiceProject = await store.TryCreateProjectAsync(
            director,
            StoreTestHelpers.ValidProjectRequest(startDate, endDate) with
            {
                Code = "TREND-JUS",
                Name = "Projekt Drejtesie",
                Ministries = ["Ministria e Drejtësisë"],
                Progress = 10,
            });

        var financeFirst = await store.TryCreateWeeklyUpdateAsync(
            director,
            new CreateWeeklyUpdateRequest(financeProject.Response!.Id, "Ekspert", 20, ProjectStatuses.Active, RiskLevels.Medium, "", "Muaji i pare"));
        SetWeeklyUpdateSubmittedAt(store, financeFirst.Response!.Id, ToMonthDate(firstMonth, 10));

        var financeSecond = await store.TryCreateWeeklyUpdateAsync(
            director,
            new CreateWeeklyUpdateRequest(financeProject.Response.Id, "Ekspert", 60, ProjectStatuses.Active, RiskLevels.Low, "", "Muaji i dyte"));
        SetWeeklyUpdateSubmittedAt(store, financeSecond.Response!.Id, ToMonthDate(secondMonth, 10));

        var justiceCurrent = await store.TryCreateWeeklyUpdateAsync(
            director,
            new CreateWeeklyUpdateRequest(justiceProject.Response!.Id, "Ekspert", 100, ProjectStatuses.Active, RiskLevels.Low, "", "Muaji aktual"));
        SetWeeklyUpdateSubmittedAt(store, justiceCurrent.Response!.Id, ToMonthDate(currentMonth, 10));

        var financeTrend = await store.GetTrend(financeContext, 3);
        var directorTrend = await store.GetTrend(director, 3);

        Assert.Equal([20, 60, 60], financeTrend.Select(point => point.Progress).ToArray());
        Assert.Equal(80, directorTrend[^1].Progress);
        Assert.All(financeTrend, point => Assert.InRange(point.Okr, 0, 100));
    }

    [Fact]
    public async Task TryCreateWeeklyUpdateAsync_UsesLoggedInUserNameForExpertName()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var staff = StoreTestHelpers.StaffContext("staff-a", "Eksperti Real", "staff-a");
        var project = await store.TryCreateProjectAsync(
            director,
            StoreTestHelpers.ValidProjectRequest() with
            {
                Code = "REAL-NAME-001",
                TeamMembers = [new WorkgroupMemberInput("Eksperti Real", WorkgroupRoles.InnovationExpert, "Agjenci", 100, "staff-a")]
            });

        var result = await store.TryCreateWeeklyUpdateAsync(
            staff,
            new CreateWeeklyUpdateRequest(project.Response!.Id, "Emri i Dikujt Tjeter", 55, ProjectStatuses.Active, RiskLevels.Medium, "", "Koment"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Eksperti Real", result.Response!.ExpertName);
    }

    [Fact]
    public async Task TryCreateWeeklyUpdateAsync_UpdatesProjectKeyResultProgress()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var objectives = new[]
        {
            new ObjectiveInput(
                "Objektiv test",
                "Test Lead",
                [
                    new KeyResultInput("KR 1", 10, 100, "%"),
                    new KeyResultInput("KR 2", 20, 50, "%"),
                ]),
        };
        var created = await store.TryCreateProjectAsync(
            context,
            StoreTestHelpers.ValidProjectRequest() with { Code = "KR-WEEKLY-001", Objectives = objectives });
        var firstKr = created.Response!.Objectives[0].KeyResults[0];
        var secondKr = created.Response.Objectives[0].KeyResults[1];

        var result = await store.TryCreateWeeklyUpdateAsync(
            context,
            new CreateWeeklyUpdateRequest(
                created.Response.Id,
                "Ekspert KR",
                45,
                ProjectStatuses.Active,
                RiskLevels.Low,
                "",
                "Perditesim KR",
                [
                    new WeeklyUpdateKeyResultInput(firstKr.Id, 75),
                    new WeeklyUpdateKeyResultInput(secondKr.Id, 25),
                ]));
        var project = await store.GetProjectById(created.Response.Id, context);

        Assert.True(result.IsSuccess);
        Assert.Equal(75, project!.Objectives[0].KeyResults[0].Progress);
        Assert.Equal(50, project.Objectives[0].KeyResults[1].Progress);
        Assert.Equal(62, project.Objectives[0].Progress);
        Assert.Equal(62, project.Progress);
    }

    [Fact]
    public async Task TryCreateWeeklyUpdateAsync_CompletesProjectAtFullProgress()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();

        var result = await store.TryCreateWeeklyUpdateAsync(
            context,
            new CreateWeeklyUpdateRequest("p1", "Ekspert Complete", 100, ProjectStatuses.Active, RiskLevels.Low, "", "Perfunduar"));
        var project = await store.GetProjectById("p1", context);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProjectStatuses.Completed, project!.Status);
        Assert.Equal(ProjectStatuses.ToLabel(ProjectStatuses.Completed), result.Response!.Status);
    }

    [Fact]
    public async Task TryUpdateWeeklyUpdateAsync_UpdatesProjectFromLatestUpdate()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var created = await store.TryCreateProjectAsync(
            context,
            StoreTestHelpers.ValidProjectRequest() with { Code = "EDIT-UPD-001", Progress = 10, Status = ProjectStatuses.Active });
        var update = await store.TryCreateWeeklyUpdateAsync(
            context,
            new CreateWeeklyUpdateRequest(created.Response!.Id, "Ekspert", 40, ProjectStatuses.Active, RiskLevels.Medium, "", "Koment fillestar"));

        var edited = await store.TryUpdateWeeklyUpdateAsync(
            context,
            update.Response!.Id,
            new CreateWeeklyUpdateRequest(created.Response.Id, "Ekspert edit", 100, ProjectStatuses.Active, RiskLevels.Low, "Pa pengesa", "Koment edit"));
        var project = await store.GetProjectById(created.Response.Id, context);

        Assert.True(edited.IsSuccess);
        Assert.Equal(100, edited.Response!.Progress);
        Assert.Equal(ProjectStatuses.ToLabel(ProjectStatuses.Completed), edited.Response.Status);
        Assert.Equal(ProjectStatuses.Completed, project!.Status);
        Assert.Equal(100, project.Progress);
    }

    [Fact]
    public async Task TryUpdateWeeklyUpdateAsync_KeepsOriginalExpertName()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var staff = StoreTestHelpers.StaffContext("staff-a", "Eksperti Real", "staff-a");
        var project = await store.TryCreateProjectAsync(
            director,
            StoreTestHelpers.ValidProjectRequest() with
            {
                Code = "KEEP-NAME-001",
                TeamMembers = [new WorkgroupMemberInput("Eksperti Real", WorkgroupRoles.InnovationExpert, "Agjenci", 100, "staff-a")]
            });
        var update = await store.TryCreateWeeklyUpdateAsync(
            staff,
            new CreateWeeklyUpdateRequest(project.Response!.Id, "Emri i Dikujt Tjeter", 40, ProjectStatuses.Active, RiskLevels.Medium, "", "Koment"));

        var edited = await store.TryUpdateWeeklyUpdateAsync(
            staff,
            update.Response!.Id,
            new CreateWeeklyUpdateRequest(project.Response.Id, "Emër i Ndryshuar", 65, ProjectStatuses.Active, RiskLevels.Low, "", "Koment i ri"));

        Assert.True(edited.IsSuccess);
        Assert.Equal("Eksperti Real", edited.Response!.ExpertName);
    }

    [Fact]
    public async Task TryDeleteWeeklyUpdateAsync_ReappliesPreviousLatestUpdate()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var objectives = new[]
        {
            new ObjectiveInput(
                "Objektiv test",
                "Test Lead",
                [
                    new KeyResultInput("KR 1", 10, 100, "%"),
                    new KeyResultInput("KR 2", 20, 50, "%"),
                ]),
        };
        var created = await store.TryCreateProjectAsync(
            context,
            StoreTestHelpers.ValidProjectRequest() with { Code = "DELETE-UPD-001", Progress = 10, Status = ProjectStatuses.Active, Objectives = objectives });
        var firstKr = created.Response!.Objectives[0].KeyResults[0];
        var secondKr = created.Response.Objectives[0].KeyResults[1];
        var first = await store.TryCreateWeeklyUpdateAsync(
            context,
            new CreateWeeklyUpdateRequest(
                created.Response.Id,
                "Ekspert 1",
                40,
                ProjectStatuses.Active,
                RiskLevels.Medium,
                "",
                "Koment 1",
                [
                    new WeeklyUpdateKeyResultInput(firstKr.Id, 45),
                    new WeeklyUpdateKeyResultInput(secondKr.Id, 20),
                ]));
        await Task.Delay(1);
        var second = await store.TryCreateWeeklyUpdateAsync(
            context,
            new CreateWeeklyUpdateRequest(
                created.Response.Id,
                "Ekspert 2",
                80,
                ProjectStatuses.AtRisk,
                RiskLevels.High,
                "Bllokim",
                "Koment 2",
                [
                    new WeeklyUpdateKeyResultInput(firstKr.Id, 90),
                    new WeeklyUpdateKeyResultInput(secondKr.Id, 40),
                ]));

        var deleted = await store.TryDeleteWeeklyUpdateAsync(context, second.Response!.Id);
        var project = await store.GetProjectById(created.Response.Id, context);
        var updates = await store.GetWeeklyUpdates(context, created.Response.Id);

        Assert.True(first.IsSuccess);
        Assert.True(deleted.IsSuccess);
        Assert.Single(updates);
        Assert.Equal(42, project!.Progress);
        Assert.Equal(ProjectStatuses.Active, project.Status);
        Assert.Equal(RiskLevels.Medium, project.Risk);
        Assert.Equal(45, project.Objectives[0].KeyResults[0].Progress);
        Assert.Equal(40, project.Objectives[0].KeyResults[1].Progress);
    }

    [Fact]
    public async Task TryDeleteWeeklyUpdateAsync_RevertsToProjectBaselineWhenNoUpdatesRemain()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = StoreTestHelpers.DirectorContext();
        var objectives = new[]
        {
            new ObjectiveInput(
                "Objektiv test",
                "Test Lead",
                [
                    new KeyResultInput("KR 1", 10, 100, "%"),
                    new KeyResultInput("KR 2", 20, 50, "%"),
                ]),
        };
        var created = await store.TryCreateProjectAsync(
            context,
            StoreTestHelpers.ValidProjectRequest() with { Code = "DELETE-ONLY-UPD-001", Progress = 10, Status = ProjectStatuses.Active, Risk = RiskLevels.Low, Objectives = objectives });
        var firstKr = created.Response!.Objectives[0].KeyResults[0];
        var secondKr = created.Response.Objectives[0].KeyResults[1];
        var update = await store.TryCreateWeeklyUpdateAsync(
            context,
            new CreateWeeklyUpdateRequest(
                created.Response.Id,
                "Ekspert",
                80,
                ProjectStatuses.AtRisk,
                RiskLevels.High,
                "Bllokim",
                "Koment",
                [
                    new WeeklyUpdateKeyResultInput(firstKr.Id, 90),
                    new WeeklyUpdateKeyResultInput(secondKr.Id, 40),
                ]));

        var deleted = await store.TryDeleteWeeklyUpdateAsync(context, update.Response!.Id);
        var project = await store.GetProjectById(created.Response.Id, context);
        var updates = await store.GetWeeklyUpdates(context, created.Response.Id);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(updates);
        Assert.Equal(10, project!.Progress);
        Assert.Equal(ProjectStatuses.Active, project.Status);
        Assert.Equal(RiskLevels.Low, project.Risk);
        Assert.Equal(10, project.Objectives[0].KeyResults[0].Progress);
        Assert.Equal(20, project.Objectives[0].KeyResults[1].Progress);
    }

    [Fact]
    public async Task TryUpdateWeeklyUpdateAsync_AllowsStaffToEditOwnUpdate()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var staff = StoreTestHelpers.StaffContext("staff-a", "Test Lead", "staff-a");
        var project = await store.TryCreateProjectAsync(
            director,
            StoreTestHelpers.ValidProjectRequest() with
            {
                Code = "OWN-UPD-001",
                TeamMembers = [new WorkgroupMemberInput("Test Lead", WorkgroupRoles.InnovationExpert, "Agjenci", 100, "staff-a")]
            });
        var update = await store.TryCreateWeeklyUpdateAsync(
            staff,
            new CreateWeeklyUpdateRequest(project.Response!.Id, "Ekspert A", 40, ProjectStatuses.Active, RiskLevels.Medium, "", "Koment"));

        var edited = await store.TryUpdateWeeklyUpdateAsync(
            staff,
            update.Response!.Id,
            new CreateWeeklyUpdateRequest(project.Response.Id, "Ekspert A", 55, ProjectStatuses.Active, RiskLevels.Low, "", "Koment i ri"));

        Assert.True(edited.IsSuccess);
        Assert.Equal(55, edited.Response!.Progress);
    }

    [Fact]
    public async Task TryUpdateWeeklyUpdateAsync_RejectsStaffEditingAnotherStaffUpdate()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var staffA = StoreTestHelpers.StaffContext("staff-a", "Test Lead", "staff-a");
        var staffB = StoreTestHelpers.StaffContext("staff-b", "Test Lead", "staff-b");
        var project = await store.TryCreateProjectAsync(
            director,
            StoreTestHelpers.ValidProjectRequest() with
            {
                Code = "OTHER-UPD-001",
                TeamMembers =
                [
                    new WorkgroupMemberInput("Test Lead", WorkgroupRoles.InnovationExpert, "Agjenci", 50, "staff-a"),
                    new WorkgroupMemberInput("Test Lead", WorkgroupRoles.InnovationExpert, "Agjenci", 50, "staff-b")
                ]
            });
        var update = await store.TryCreateWeeklyUpdateAsync(
            staffA,
            new CreateWeeklyUpdateRequest(project.Response!.Id, "Ekspert A", 40, ProjectStatuses.Active, RiskLevels.Medium, "", "Koment"));

        var edited = await store.TryUpdateWeeklyUpdateAsync(
            staffB,
            update.Response!.Id,
            new CreateWeeklyUpdateRequest(project.Response.Id, "Ekspert B", 75, ProjectStatuses.Active, RiskLevels.Low, "", "Ndryshim"));

        Assert.False(edited.IsSuccess);
        Assert.Contains("vetem perditesimet", edited.Error);
    }

    [Fact]
    public async Task TryDeleteWeeklyUpdateAsync_RejectsStaffDeletingAnotherStaffUpdate()
    {
        var store = StoreTestHelpers.CreateStore();
        var director = StoreTestHelpers.DirectorContext();
        var staffA = StoreTestHelpers.StaffContext("staff-a", "Test Lead", "staff-a");
        var staffB = StoreTestHelpers.StaffContext("staff-b", "Test Lead", "staff-b");
        var project = await store.TryCreateProjectAsync(
            director,
            StoreTestHelpers.ValidProjectRequest() with
            {
                Code = "DELETE-OTHER-001",
                TeamMembers =
                [
                    new WorkgroupMemberInput("Test Lead", WorkgroupRoles.InnovationExpert, "Agjenci", 50, "staff-a"),
                    new WorkgroupMemberInput("Test Lead", WorkgroupRoles.InnovationExpert, "Agjenci", 50, "staff-b")
                ]
            });
        var update = await store.TryCreateWeeklyUpdateAsync(
            staffA,
            new CreateWeeklyUpdateRequest(project.Response!.Id, "Ekspert A", 40, ProjectStatuses.Active, RiskLevels.Medium, "", "Koment"));

        var deleted = await store.TryDeleteWeeklyUpdateAsync(staffB, update.Response!.Id);
        var updates = await store.GetWeeklyUpdates(director, project.Response.Id);

        Assert.False(deleted.IsSuccess);
        Assert.Single(updates);
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

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

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

    private static void ClearProjectsAndUpdates(InnovationDashboardStore store)
    {
        GetPrivateList(store, "_projects").Clear();
        GetPrivateList(store, "_updates").Clear();
    }

    private static IList GetPrivateList(InnovationDashboardStore store, string fieldName)
    {
        var field = typeof(InnovationDashboardStore).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingFieldException(nameof(InnovationDashboardStore), fieldName);
        return (IList)field.GetValue(store)!;
    }

    private static void SetWeeklyUpdateSubmittedAt(InnovationDashboardStore store, string updateId, DateTimeOffset submittedAt)
    {
        var update = GetPrivateList(store, "_updates")
            .Cast<object>()
            .First(item => string.Equals(GetProperty<string>(item, "Id"), updateId, StringComparison.OrdinalIgnoreCase));
        SetProperty(update, "SubmittedAt", submittedAt);
    }

    private static DateTimeOffset ToMonthDate(DateOnly month, int day) =>
        new(month.Year, month.Month, day, 12, 0, 0, TimeSpan.Zero);

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMemberException(instance.GetType().Name, propertyName);
        return (T)property.GetValue(instance)!;
    }

    private static void SetProperty<T>(object instance, string propertyName, T value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMemberException(instance.GetType().Name, propertyName);
        property.SetValue(instance, value);
    }
}
