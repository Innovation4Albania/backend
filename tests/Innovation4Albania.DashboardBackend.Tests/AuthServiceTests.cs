using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services;
using Microsoft.Extensions.Configuration;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public void ValidateViewLink_AllowsMinistryRepresentativeWithKnownMinistry()
    {
        var service = CreateService();
        var request = new LoginRequest(ApplicationRoles.StafMinistrie, "Ministria e Financave", "Përfaqësues Ministrie");

        var error = service.ValidateViewLink(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateViewLink_AllowsInstitutionRepresentativeWithKnownScope()
    {
        var service = CreateService();
        var request = new LoginRequest(ApplicationRoles.PerfaqesuesInstitucioni, "AKSHI", "Përfaqësues Institucioni");

        var error = service.ValidateViewLink(request);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateViewLink_RejectsMinisterWithoutRequiredMinistry()
    {
        var service = CreateService();
        var request = new LoginRequest(ApplicationRoles.Minister, null, "Ministër");

        var error = service.ValidateViewLink(request);

        Assert.NotNull(error);
        Assert.Contains("ministr", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateViewLink_AllowsMinisterWithKnownMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var service = CreateService(store: store);
        var request = new LoginRequest(ApplicationRoles.Minister, store.GetMinistries()[0], "Ministër");

        var error = service.ValidateViewLink(request);

        Assert.Null(error);
    }

    [Fact]
    public async Task TryLoginAsync_RejectsCredentialLoginForRepresentativeAccount()
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

        Assert.False(result.IsSuccess);
        Assert.Null(result.Response);
    }

    [Theory]
    [InlineData(ApplicationRoles.StafAgjencie)]
    [InlineData(ApplicationRoles.Ekspert)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ)]
    [InlineData(ApplicationRoles.EkspertProjekteBe)]
    [InlineData(ApplicationRoles.Specialist)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik)]
    [InlineData(ApplicationRoles.Admin)]
    [InlineData(ApplicationRoles.StafMinistrie)]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni)]
    public async Task TryLoginAsync_RejectsInnovationAccountsThroughInnovation4AlbaniaOption(string accountRole)
    {
        var ministry = accountRole is ApplicationRoles.StafMinistrie or ApplicationRoles.PerfaqesuesInstitucioni ? "Ministria e Financave" : null;
        var account = InMemoryUserRepository.Account("account-1", "innovation.user", "password123", accountRole, "Innovation User", ministry);
        var service = CreateService(users: new InMemoryUserRepository(account));

        var result = await service.TryLoginAsync(new LoginRequest(ApplicationRoles.DrejtorAgjencie, null, null, "innovation.user", "password123"));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Response);
    }

    [Theory]
    [InlineData(ApplicationRoles.Kryeminister, null)]
    [InlineData(ApplicationRoles.Minister, "Ministria e Financave")]
    [InlineData(ApplicationRoles.MinisterEkonomiseInovacionit, "Ministria e EkonomisÃ« dhe Inovacionit")]
    public async Task TryLoginAsync_RejectsExecutiveAndMinisterAccountsWithCredentials(string accountRole, string? expectedMinistry)
    {
        var account = InMemoryUserRepository.Account("account-1", "view.user", "password123", accountRole, "View User", expectedMinistry);
        var service = CreateService(users: new InMemoryUserRepository(account));

        var result = await service.TryLoginAsync(new LoginRequest(accountRole, null, null, "view.user", "password123"));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Response);
    }

    [Theory]
    [InlineData(ApplicationRoles.Kryeminister)]
    [InlineData(ApplicationRoles.Minister)]
    [InlineData(ApplicationRoles.MinisterEkonomiseInovacionit)]
    [InlineData(ApplicationRoles.Admin)]
    [InlineData(ApplicationRoles.DrejtorAgjencie)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik)]
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit)]
    [InlineData(ApplicationRoles.StafAgjencie)]
    [InlineData(ApplicationRoles.Ekspert)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ)]
    [InlineData(ApplicationRoles.EkspertProjekteBe)]
    [InlineData(ApplicationRoles.Specialist)]
    [InlineData(ApplicationRoles.StafMinistrie)]
    [InlineData(ApplicationRoles.PerfaqesuesInstitucioni)]
    public async Task TryLoginAsync_RejectsInactiveAccountsForEveryManagedRole(string accountRole)
    {
        var ministry = accountRole is ApplicationRoles.Minister or ApplicationRoles.StafMinistrie or ApplicationRoles.PerfaqesuesInstitucioni ? "Ministria e Financave" : null;
        var account = InMemoryUserRepository.Account("account-1", "inactive.user", "password123", accountRole, "Inactive User", ministry) with
        {
            IsActive = false
        };
        var service = CreateService(users: new InMemoryUserRepository(account));
        var loginRole = ApplicationRoles.CanUseInteractiveLogin(accountRole) ? accountRole : ApplicationRoles.DrejtorAgjencie;

        var result = await service.TryLoginAsync(new LoginRequest(loginRole, null, null, "inactive.user", "password123"));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Response);
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
    [InlineData(ApplicationRoles.DrejtorAgjencie)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik)]
    public async Task GetManagedUsersAsync_ProjectDirectorsCanReadAccountsForProjectTeams(string directorRole)
    {
        var account = InMemoryUserRepository.Account("expert-1", "expert.test", "password123", ApplicationRoles.Ekspert, "Ekspert Test");
        var users = new InMemoryUserRepository(account);
        var service = CreateService(users: users);

        var result = await service.GetManagedUsersAsync(UserContext.From(directorRole, null));

        Assert.Contains(result, user => user.Id == "expert-1");
    }

    [Theory]
    [InlineData(ApplicationRoles.Kryeminister)]
    [InlineData(ApplicationRoles.MinisterEkonomiseInovacionit)]
    [InlineData(ApplicationRoles.Admin)]
    [InlineData(ApplicationRoles.DrejtorAgjencie)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik)]
    [InlineData(ApplicationRoles.DrejtorEkosistemiStartupeveLehtesuesve)]
    [InlineData(ApplicationRoles.DrejtorFinancimiAlternativNderkombetarizimit)]
    [InlineData(ApplicationRoles.StafAgjencie)]
    [InlineData(ApplicationRoles.Ekspert)]
    [InlineData(ApplicationRoles.EkspertEkosistemiStartupeve)]
    [InlineData(ApplicationRoles.EkspertProgrametMbeshtetjes)]
    [InlineData(ApplicationRoles.EkspertFinancimiAlternativ)]
    [InlineData(ApplicationRoles.EkspertProjekteBe)]
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
    public async Task CreateUserAsync_AdminCreatesInstitutionRepresentativeWithScope()
    {
        var users = new InMemoryUserRepository();
        var service = CreateService(users: users);

        var result = await service.CreateUserAsync(
            UserContext.From(ApplicationRoles.Admin, null),
            new CreateUserRequest("Përfaqësues Institucioni", "institution.rep", "password123", ApplicationRoles.PerfaqesuesInstitucioni, "AKSHI"));

        Assert.True(result.IsSuccess);
        Assert.Equal("institution.rep", result.Response!.Username);
        Assert.Equal(ApplicationRoles.PerfaqesuesInstitucioni, result.Response.Role);
        Assert.Equal("AKSHI", result.Response.Ministry);
        Assert.NotNull(await users.GetUserByUsername("institution.rep"));
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
    public async Task DeleteUserAsync_AdminPermanentlyRemovesManagedUser()
    {
        var account = InMemoryUserRepository.Account("expert-1", "expert.old", "password123", ApplicationRoles.Ekspert, "Ekspert Test");
        var users = new InMemoryUserRepository(account);
        var service = CreateService(users: users);

        var result = await service.DeleteUserAsync(UserContext.From(ApplicationRoles.Admin, null, "admin", "Admin", "admin-1"), account.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(await users.GetUserById(account.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_RejectsDeletingCurrentAccount()
    {
        var account = InMemoryUserRepository.Account("admin-1", "admin", "password123", ApplicationRoles.Admin, "Admin Test");
        var users = new InMemoryUserRepository(account);
        var service = CreateService(users: users);

        var result = await service.DeleteUserAsync(UserContext.From(ApplicationRoles.Admin, null, "admin", "Admin Test", "admin-1"), account.Id);

        Assert.False(result.IsSuccess);
        Assert.NotNull(await users.GetUserById(account.Id));
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

    [Fact]
    public async Task ChangeOwnCredentialsAsync_RejectsCaseOnlyUsernameChangeWithoutPassword()
    {
        var account = InMemoryUserRepository.Account("expert-1", "expert.old", "password123", ApplicationRoles.StafAgjencie, "Ekspert Test");
        var users = new InMemoryUserRepository(account);
        var service = CreateService(users: users);

        var result = await service.ChangeOwnCredentialsAsync(
            UserContext.From(ApplicationRoles.StafAgjencie, null, "expert.old"),
            new ChangeOwnCredentialsRequest("password123", "EXPERT.OLD", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("expert.old", (await users.GetUserById(account.Id))!.Username);
    }

    [Theory]
    [InlineData(ApplicationRoles.DrejtorAgjencie)]
    [InlineData(ApplicationRoles.DrejtorInovacioniPublik)]
    public async Task ChangeOwnCredentialsAsync_DirectorsCanChangeOwnPasswordButCredentialLoginRemainsDisabled(string directorRole)
    {
        var account = InMemoryUserRepository.Account("director-1", "director.old", "password123", directorRole, "Drejtor Test");
        var users = new InMemoryUserRepository(account);
        var service = CreateService(users: users);

        var changed = await service.ChangeOwnCredentialsAsync(
            UserContext.From(directorRole, null, "director.old"),
            new ChangeOwnCredentialsRequest("password123", "director.old", "password456"));
        var login = await service.TryLoginAsync(new LoginRequest(ApplicationRoles.DrejtorAgjencie, null, null, "director.old", "password456"));

        Assert.True(changed.IsSuccess);
        Assert.False(login.IsSuccess);
        Assert.Null(login.Response);
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
