using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class ApiIntegrationTests : IClassFixture<DashboardApiFactory>
{
    private readonly DashboardApiFactory _factory;

    public ApiIntegrationTests(DashboardApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Protected_endpoint_requires_authentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_create_update_and_delete_project_flow_updates_http_state()
    {
        using var client = await CreateAuthenticatedDirectorClient();
        var projectRequest = BuildProjectRequest($"INT-{Guid.NewGuid():N}");

        var createResponse = await client.PostAsJsonAsync("/api/projects", projectRequest);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);
        Assert.Equal(projectRequest.Code, project.Code);
        Assert.Equal(0, project.Progress);

        var getResponse = await client.GetAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateRequest = new CreateWeeklyUpdateRequest(
            project.Id,
            "Ekspert Integrimi",
            64,
            ProjectStatuses.Active,
            RiskLevels.Low,
            string.Empty,
            "Progres i testuar përmes HTTP.");

        var updateResponse = await client.PostAsJsonAsync("/api/updates", updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var update = await updateResponse.Content.ReadFromJsonAsync<WeeklyUpdateResponse>();
        Assert.NotNull(update);
        Assert.Equal(project.Id, update.ProjectId);
        Assert.Equal(64, update.Progress);

        var updatedProject = await client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{project.Id}");
        Assert.NotNull(updatedProject);
        Assert.Equal(64, updatedProject.Progress);

        var updates = await client.GetFromJsonAsync<List<WeeklyUpdateResponse>>($"/api/updates?projectId={project.Id}");
        Assert.NotNull(updates);
        Assert.Contains(updates, item => item.Id == update.Id);

        var deleteResponse = await client.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var deletedProjectResponse = await client.GetAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedProjectResponse.StatusCode);

        var remainingUpdates = await client.GetFromJsonAsync<List<WeeklyUpdateResponse>>($"/api/updates?projectId={project.Id}");
        Assert.NotNull(remainingUpdates);
        Assert.DoesNotContain(remainingUpdates, item => item.Id == update.Id);
    }

    [Fact]
    public async Task Invalid_project_request_returns_structured_api_error()
    {
        using var client = await CreateAuthenticatedDirectorClient();
        var invalidRequest = BuildProjectRequest(string.Empty);

        var response = await client.PostAsJsonAsync("/api/projects", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("project_create_failed", error.Code);
        Assert.NotEmpty(error.Message);
    }

    [Fact]
    public async Task Director_can_create_list_and_deactivate_expert_account()
    {
        using var client = await CreateAuthenticatedDirectorClient();
        var username = $"expert-{Guid.NewGuid():N}";
        var request = new CreateUserRequest("Ekspert Integrimi", username, "password123", ApplicationRoles.StafAgjencie);

        var createResponse = await client.PostAsJsonAsync("/api/auth/users", request);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ManagedUserResponse>();
        Assert.NotNull(created);
        Assert.Equal(username, created.Username);

        var accounts = await client.GetFromJsonAsync<List<ManagedUserResponse>>("/api/auth/users");
        Assert.NotNull(accounts);
        Assert.Contains(accounts, item => item.Id == created.Id);

        var deactivateResponse = await client.DeleteAsync($"/api/auth/users/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedDirectorClient()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                ApplicationRoles.DrejtorAgjencie,
                null,
                "Drejtor Integrimi",
                DashboardApiFactory.DirectorUsername,
                DashboardApiFactory.DirectorPassword));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    private static CreateProjectRequest BuildProjectRequest(string code) =>
        new(
            code,
            "Projekt integrimi HTTP",
            "Projekt i krijuar nga suite integrimi.",
            ["Ministria e Financave"],
            "Drejtoria e Inovacionit",
            ProjectStatuses.Active,
            ProjectPriorities.High,
            ProjectSectors.Digitalization,
            6,
            1,
            DateTimeOffset.UtcNow.Date,
            DateTimeOffset.UtcNow.Date.AddMonths(3),
            0,
            RiskLevels.Medium,
            ["Drejtues Integrimi"],
            [new WorkgroupMemberInput("Drejtues Integrimi", WorkgroupRoles.ProjectLead, "Test", 100)],
            "Drejtues Integrimi",
            14,
            []);
}

public sealed class DashboardApiFactory : WebApplicationFactory<Program>
{
    public const string DirectorUsername = "integration-director";
    public const string DirectorPassword = "integration-password";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "integration-test-signing-key-with-at-least-32-bytes",
                ["Database:ConnectionString"] = string.Empty,
                ["ConnectionStrings:DefaultConnection"] = string.Empty
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDashboardStorePersistence>();
            services.AddSingleton<IDashboardStorePersistence, InMemoryDashboardStorePersistence>();
            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository>(new InMemoryUserRepository(
                InMemoryUserRepository.Account(
                    "integration-director-id",
                    DirectorUsername,
                    DirectorPassword,
                    ApplicationRoles.DrejtorAgjencie,
                    "Drejtor Integrimi")));
        });
    }

    private sealed class InMemoryDashboardStorePersistence : IDashboardStorePersistence
    {
        public bool IsConfigured => false;

        public Task<string?> LoadSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task SaveSnapshotAsync(string payload, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
