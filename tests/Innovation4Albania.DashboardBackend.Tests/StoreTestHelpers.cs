using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Models;
using Microsoft.Extensions.Logging;

namespace Innovation4Albania.DashboardBackend.Tests;

internal static class StoreTestHelpers
{
    public static InnovationDashboardStore CreateStore() =>
        new(new TestHttpClientFactory(), new TestLogger<InnovationDashboardStore>());

    public static UserContext DirectorContext() => UserContext.From(ApplicationRoles.DrejtorAgjencie, null);

    public static UserContext StaffContext() => UserContext.From(ApplicationRoles.StafAgjencie, null);

    public static CreateProjectRequest ValidProjectRequest(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string status = ProjectStatuses.Active,
        string priority = ProjectPriorities.High,
        string sector = ProjectSectors.Governance,
        string risk = RiskLevels.Medium) =>
        new(
            "TEST-001",
            "Projekt test",
            "Pershkrim test",
            ["Ministria e Financave"],
            "Agjenci test",
            status,
            priority,
            sector,
            5,
            2,
            startDate ?? DateTimeOffset.UtcNow.AddDays(-10),
            endDate ?? DateTimeOffset.UtcNow.AddDays(20),
            35,
            new ProjectOkr(70, 75, 80, 65, 60),
            risk,
            ["Test Lead"],
            [new WorkgroupMemberInput("Test Lead", WorkgroupRoles.ProjectLead, "Njesi test", 80)],
            "Test Lead",
            14,
            []);

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
