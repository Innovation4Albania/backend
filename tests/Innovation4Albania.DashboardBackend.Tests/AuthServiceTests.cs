using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services;
using Microsoft.Extensions.Configuration;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public void ValidateViewLink_RejectsMinistryRepresentative()
    {
        var service = CreateService();
        var request = new LoginRequest(ApplicationRoles.StafMinistrie, "Ministria e Financave", "Përfaqësues Ministrie");

        var error = service.ValidateViewLink(request);

        Assert.NotNull(error);
        Assert.Contains("login", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateViewLink_RejectsMinisterWithoutMinistry()
    {
        var service = CreateService();
        var request = new LoginRequest(ApplicationRoles.Minister, null, "Ministër");

        var error = service.ValidateViewLink(request);

        Assert.NotNull(error);
        Assert.Contains("ministrie", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateViewLink_AcceptsMinisterWithKnownMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var service = CreateService(store: store);
        var request = new LoginRequest(ApplicationRoles.Minister, store.GetMinistries()[0], "Ministër");

        var error = service.ValidateViewLink(request);

        Assert.Null(error);
    }

    [Fact]
    public async Task TryLoginAsync_UsesMinistryFromStoredRepresentativeAccount()
    {
        var account = InMemoryUserRepository.Account(
            "rep-1",
            "finance.rep",
            "password123",
            ApplicationRoles.StafMinistrie,
            "Përfaqësues Financash",
            "Ministria e Financave");
        var service = CreateService(users: new InMemoryUserRepository(account));

        var result = await service.TryLoginAsync(new LoginRequest(ApplicationRoles.StafMinistrie, null, null, "finance.rep", "password123"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ministria e Financave", result.Response!.User.Ministry);
        Assert.Equal("Përfaqësues Financash", result.Response.User.Name);
    }

    [Theory]
    [InlineData(ApplicationRoles.StafAgjencie)]
    [InlineData(ApplicationRoles.Ekspert)]
    [InlineData(ApplicationRoles.Specialist)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik)]
    [InlineData(ApplicationRoles.Admin)]
    public async Task TryLoginAsync_AllowsInnovationAccountsThroughInnovation4AlbaniaOption(string accountRole)
    {
        var account = InMemoryUserRepository.Account("account-1", "innovation.user", "password123", accountRole, "Innovation User");
        var service = CreateService(users: new InMemoryUserRepository(account));

        var result = await service.TryLoginAsync(new LoginRequest(ApplicationRoles.DrejtorAgjencie, null, null, "innovation.user", "password123"));

        Assert.True(result.IsSuccess);
        Assert.Equal(accountRole, result.Response!.User.Role);
    }

    [Fact]
    public async Task CreateUserAsync_AdminCreatesExpertAccount()
    {
        var users = new InMemoryUserRepository();
        var service = CreateService(users: users);

        var result = await service.CreateUserAsync(
            UserContext.From(ApplicationRoles.Admin, null),
            new CreateUserRequest("Ekspert Test", "expert.test", "password123", ApplicationRoles.Ekspert));

        Assert.True(result.IsSuccess);
        Assert.Equal("expert.test", result.Response!.Username);
        Assert.NotNull(await users.GetUserByUsername("expert.test"));
    }

    [Theory]
    [InlineData(ApplicationRoles.Kryeminister)]
    [InlineData(ApplicationRoles.Admin)]
    [InlineData(ApplicationRoles.DrejtorAgjencie)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik)]
    [InlineData(ApplicationRoles.StafAgjencie)]
    [InlineData(ApplicationRoles.Ekspert)]
    [InlineData(ApplicationRoles.Specialist)]
    public async Task CreateUserAsync_AdminCreatesManagedAccounts(string role)
    {
        var users = new InMemoryUserRepository();
        var service = CreateService(users: users);
        var username = $"account.{role}";

        var result = await service.CreateUserAsync(
            UserContext.From(ApplicationRoles.Admin, null),
            new CreateUserRequest("PunonjÃ«s Test", username, "password123", role));

        Assert.True(result.IsSuccess);
        Assert.Equal(username, result.Response!.Username);
        Assert.Equal(role, result.Response.Role);
        Assert.NotNull(await users.GetUserByUsername(username));
    }

    [Fact]
    public async Task CreateUserAsync_AdminCreatesMinistryRepresentativeWithMinistry()
    {
        var users = new InMemoryUserRepository();
        var service = CreateService(users: users);

        var result = await service.CreateUserAsync(
            UserContext.From(ApplicationRoles.Admin, null),
            new CreateUserRequest("Përfaqësues Test", "rep.test", "password123", ApplicationRoles.StafMinistrie, "Ministria e Financave"));

        Assert.True(result.IsSuccess);
        Assert.Equal("rep.test", result.Response!.Username);
        Assert.Equal(ApplicationRoles.StafMinistrie, result.Response.Role);
        Assert.Equal("Ministria e Financave", result.Response.Ministry);
        Assert.NotNull(await users.GetUserByUsername("rep.test"));
    }

    [Fact]
    public async Task CreateUserAsync_AdminCreatesMinisterWithMinistry()
    {
        var users = new InMemoryUserRepository();
        var service = CreateService(users: users);

        var result = await service.CreateUserAsync(
            UserContext.From(ApplicationRoles.Admin, null),
            new CreateUserRequest("Ministër Test", "finance.minister", "password123", ApplicationRoles.Minister, "Ministria e Financave"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ministria e Financave", result.Response!.Ministry);
    }

    [Fact]
    public async Task UpdateUserAsync_AdminUpdatesManagedUserIdentityPasswordAndMinistry()
    {
        var account = InMemoryUserRepository.Account("expert-1", "expert.old", "password123", ApplicationRoles.Ekspert, "Ekspert Test");
        var users = new InMemoryUserRepository(account);
        var service = CreateService(users: users);

        var result = await service.UpdateUserAsync(
            UserContext.From(ApplicationRoles.Admin, null),
            account.Id,
            new UpdateManagedUserRequest("Ministër i Përditësuar", "minister.new", "password456", ApplicationRoles.Minister, "Ministria e Financave"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ministër i Përditësuar", result.Response!.FullName);
        Assert.Equal("minister.new", result.Response.Username);
        Assert.Equal(ApplicationRoles.Minister, result.Response.Role);
        Assert.Equal("Ministria e Financave", result.Response.Ministry);
        var updated = await users.GetUserByUsername("minister.new");
        Assert.NotNull(updated);
        Assert.True(BCrypt.Net.BCrypt.Verify("password456", updated!.PasswordHash));
    }

    [Fact]
    public async Task ChangeOwnCredentialsAsync_UpdatesUsernameAndPassword()
    {
        var account = InMemoryUserRepository.Account("expert-1", "expert.old", "password123", ApplicationRoles.StafAgjencie, "Ekspert Test");
        var users = new InMemoryUserRepository(account);
        var service = CreateService(users: users);

        var result = await service.ChangeOwnCredentialsAsync(
            UserContext.From(ApplicationRoles.StafAgjencie, null, "expert.old"),
            new ChangeOwnCredentialsRequest("password123", "expert.new", "password456"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(await users.GetUserByUsername("expert.new"));
        Assert.True(BCrypt.Net.BCrypt.Verify("password456", (await users.GetUserByUsername("expert.new"))!.PasswordHash));
    }

    private static AuthService CreateService(Innovation4Albania.DashboardBackend.Api.Data.InnovationDashboardStore? store = null, IUserRepository? users = null)
    {
        store ??= StoreTestHelpers.CreateStore();
        return new AuthService(new InnovationDashboardRepository(store), users ?? new InMemoryUserRepository(), CreateConfiguration());
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes"
            })
            .Build();
}
