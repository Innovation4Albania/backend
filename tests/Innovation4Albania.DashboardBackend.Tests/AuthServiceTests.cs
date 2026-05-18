using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services;
using Microsoft.Extensions.Configuration;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public void ValidateViewLink_RejectsMinistryStaffWithoutMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var repository = new InnovationDashboardRepository(store);
        var service = new AuthService(repository, CreateConfiguration());
        var request = new LoginRequest(ApplicationRoles.StafMinistrie, null, "Staf Ministrie");

        var error = service.ValidateViewLink(request);

        Assert.NotNull(error);
        Assert.Contains("ministrie", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateViewLink_AcceptsMinistryStaffWithKnownMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var repository = new InnovationDashboardRepository(store);
        var service = new AuthService(repository, CreateConfiguration());
        var request = new LoginRequest(ApplicationRoles.StafMinistrie, store.GetMinistries()[0], "Staf Ministrie");

        var error = service.ValidateViewLink(request);

        Assert.Null(error);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes"
            })
            .Build();
}
