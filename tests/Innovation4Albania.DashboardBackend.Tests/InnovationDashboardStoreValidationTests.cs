using System.Reflection;
using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreValidationTests
{
    [Fact]
    public void TryValidateProjectRequest_AcceptsValidRequest()
    {
        var (isValid, error) = InvokeTryValidateProjectRequest(StoreTestHelpers.ValidProjectRequest());

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsMissingName()
    {
        var request = StoreTestHelpers.ValidProjectRequest() with { Name = "" };

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("Kodi dhe emri", error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsInvalidStatus()
    {
        var request = StoreTestHelpers.ValidProjectRequest(status: "unknown");

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("Statusi", error);
    }

    [Fact]
    public void TryValidateProjectRequest_RejectsEndDateBeforeStartDate()
    {
        var start = DateTimeOffset.UtcNow.AddDays(5);
        var end = DateTimeOffset.UtcNow.AddDays(1);
        var request = StoreTestHelpers.ValidProjectRequest(start, end);

        var (isValid, error) = InvokeTryValidateProjectRequest(request);

        Assert.False(isValid);
        Assert.Contains("Data e mbylljes", error);
    }

    [Fact]
    public void IsValidContext_RejectsMinistryStaffWithoutMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var context = UserContext.From(ApplicationRoles.StafMinistrie, null);

        var isValid = store.IsValidContext(context, out var error);

        Assert.False(isValid);
        Assert.Contains("ministrie", error);
    }

    [Fact]
    public void IsValidContext_AcceptsMinistryStaffWithKnownMinistry()
    {
        var store = StoreTestHelpers.CreateStore();
        var ministry = store.GetMinistries()[0];
        var context = UserContext.From(ApplicationRoles.StafMinistrie, ministry);

        var isValid = store.IsValidContext(context, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    private static (bool IsValid, string? Error) InvokeTryValidateProjectRequest(CreateProjectRequest request)
    {
        var method = typeof(InnovationDashboardStore).GetMethod("TryValidateProjectRequest", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(InnovationDashboardStore), "TryValidateProjectRequest");
        object?[] args = [request, null];

        var result = (bool)method.Invoke(null, args)!;

        return (result, args[1] as string);
    }
}
