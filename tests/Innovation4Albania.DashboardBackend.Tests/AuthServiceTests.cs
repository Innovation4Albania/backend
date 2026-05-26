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

    [Fact]
    public async Task CreateUserAsync_DirectorCreatesExpertAccount()
    {
        var users = new InMemoryUserRepository();
        var service = CreateService(users: users);

        var result = await service.CreateUserAsync(
            StoreTestHelpers.DirectorContext(),
            new CreateUserRequest("Ekspert Test", "expert.test", "password123", ApplicationRoles.StafAgjencie));

        Assert.True(result.IsSuccess);
        Assert.Equal("expert.test", result.Response!.Username);
        Assert.NotNull(await users.GetUserByUsername("expert.test"));
    }

    [Fact]
    public async Task CreateUserAsync_DirectorCreatesMinistryRepresentativeWithMinistry()
    {
        var users = new InMemoryUserRepository();
        var service = CreateService(users: users);

        var result = await service.CreateUserAsync(
            StoreTestHelpers.DirectorContext(),
            new CreateUserRequest("Përfaqësues Test", "finance.rep", "password123", ApplicationRoles.StafMinistrie, "Ministria e Financave"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ministria e Financave", result.Response!.Ministry);
    }

    [Fact]
    public async Task UpdateUserAsync_DirectorUpdatesManagedUserIdentityAndPassword()
    {
        var account = InMemoryUserRepository.Account("expert-1", "expert.old", "password123", ApplicationRoles.StafAgjencie, "Ekspert Test");
        var users = new InMemoryUserRepository(account);
        var service = CreateService(users: users);

        var result = await service.UpdateUserAsync(
            StoreTestHelpers.DirectorContext(),
            account.Id,
            new UpdateManagedUserRequest("Ekspert i Përditësuar", "expert.new", "password456"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ekspert i Përditësuar", result.Response!.FullName);
        Assert.Equal("expert.new", result.Response.Username);
        var updated = await users.GetUserByUsername("expert.new");
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
