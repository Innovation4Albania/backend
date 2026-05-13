using System.Collections;
using System.Reflection;
using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreCalculationTests
{
    [Fact]
    public void CalculateExpectedProgress_ReturnsZeroBeforeStart()
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var end = DateTimeOffset.UtcNow.AddDays(12);

        var progress = InvokeCalculateExpectedProgress(start, end);

        Assert.Equal(0, progress);
    }

    [Fact]
    public void CalculateExpectedProgress_ReturnsOneHundredAfterEnd()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-12);
        var end = DateTimeOffset.UtcNow.AddDays(-2);

        var progress = InvokeCalculateExpectedProgress(start, end);

        Assert.Equal(100, progress);
    }

    [Fact]
    public void CalculateExpectedProgress_ReturnsAboutHalfwayDuringWindow()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-10);
        var end = DateTimeOffset.UtcNow.AddDays(10);

        var progress = InvokeCalculateExpectedProgress(start, end);

        Assert.InRange(progress, 49, 51);
    }

    [Fact]
    public void CalculateRiskScore_UsesRiskOkrDelayAndDeadlinePenalties()
    {
        var store = StoreTestHelpers.CreateStore();
        var project = GetProjectState(store, "p1");
        var response = InvokeToResponse(project);

        var actualScore = InvokeCalculateRiskScore(project, response);
        var expectedScore = CalculateExpectedRiskScore(project, response);

        Assert.Equal(expectedScore, actualScore);
        Assert.InRange(actualScore, 0, 100);
    }

    private static int InvokeCalculateExpectedProgress(DateTimeOffset start, DateTimeOffset end)
    {
        var method = typeof(InnovationDashboardStore).GetMethod("CalculateExpectedProgress", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(InnovationDashboardStore), "CalculateExpectedProgress");
        return (int)method.Invoke(null, [start, end])!;
    }

    private static object GetProjectState(InnovationDashboardStore store, string projectId)
    {
        var field = typeof(InnovationDashboardStore).GetField("_projects", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingFieldException(nameof(InnovationDashboardStore), "_projects");
        var projects = (IEnumerable)field.GetValue(store)!;

        return projects.Cast<object>().First(project =>
            string.Equals(GetProperty<string>(project, "Id"), projectId, StringComparison.OrdinalIgnoreCase));
    }

    private static ProjectResponse InvokeToResponse(object project)
    {
        var method = typeof(InnovationDashboardStore).GetMethod("ToResponse", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(InnovationDashboardStore), "ToResponse");
        return (ProjectResponse)method.Invoke(null, [project])!;
    }

    private static int InvokeCalculateRiskScore(object project, ProjectResponse response)
    {
        var method = typeof(InnovationDashboardStore).GetMethod("CalculateRiskScore", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(InnovationDashboardStore), "CalculateRiskScore");
        return (int)method.Invoke(null, [project, response])!;
    }

    private static int CalculateExpectedRiskScore(object project, ProjectResponse response)
    {
        var riskBase = GetProperty<string>(project, "Risk") switch
        {
            RiskLevels.Critical => 75,
            RiskLevels.High => 55,
            RiskLevels.Medium => 30,
            _ => 8
        };
        var progress = GetProperty<int>(project, "Progress");
        var okrPenalty = Math.Max(0, 100 - response.OkrAverage) * 0.35;
        var progressPenalty = Math.Max(0, response.ExpectedProgress - progress) * 0.45;
        var delayPenalty = Math.Min(20, Math.Max(0, response.DelayDays) * 0.8);
        var deadlinePenalty = response.DaysRemaining <= 30 && progress < 90 ? 10 : 0;

        return Math.Clamp((int)Math.Round(riskBase + okrPenalty + progressPenalty + delayPenalty + deadlinePenalty), 0, 100);
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMemberException(instance.GetType().Name, propertyName);
        return (T)property.GetValue(instance)!;
    }
}
