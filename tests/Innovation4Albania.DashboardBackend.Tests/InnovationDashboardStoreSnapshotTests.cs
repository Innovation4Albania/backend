using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreSnapshotTests
{
    [Fact]
    public async Task InitializeAsync_RestoresStoredProjectOkrWithoutRecalculating()
    {
        var store = StoreTestHelpers.CreateStore(new SnapshotPersistence("""
            {
              "projects": [
                {
                  "id": "p-snapshot",
                  "code": "SNAP-001",
                  "name": "Projekt snapshot",
                  "description": "Projekt nga snapshot",
                  "ministries": [],
                  "agency": "Agjenci test",
                  "status": "active",
                  "priority": "medium",
                  "sector": "digitalization",
                  "totalPhases": 6,
                  "currentPhase": 2,
                  "startDate": "2026-01-01T00:00:00+00:00",
                  "endDate": "2026-12-31T00:00:00+00:00",
                  "progress": 40,
                  "okr": { "deadlines": 1, "quality": 2, "impact": 3, "dynamics": 4 },
                  "risk": "medium",
                  "team": [],
                  "teamMembers": [],
                  "lead": "Drejtues test",
                  "updateCadenceDays": 14,
                  "lastUpdated": "2026-01-10T00:00:00+00:00",
                  "objectives": []
                }
              ],
              "portfolioObjectives": [],
              "updates": [],
              "changeProposals": []
            }
            """));

        await store.InitializeAsync();
        var project = await store.GetProjectById("p-snapshot", StoreTestHelpers.DirectorContext());

        Assert.NotNull(project);
        Assert.Equal(new ProjectOkr(1, 2, 3, 4), project!.Okr);
    }

    private sealed class SnapshotPersistence(string payload) : IDashboardStorePersistence
    {
        public bool IsConfigured => true;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string?> LoadSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(payload);

        public Task SaveSnapshotAsync(string payload, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
