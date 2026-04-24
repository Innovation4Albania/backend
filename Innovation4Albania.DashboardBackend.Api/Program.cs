using System.Text.Json.Serialization;
using Innovation4Albania.DashboardBackend.Api.Data;
using Innovation4Albania.DashboardBackend.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var defaultOrigins = new[]
        {
            "http://localhost:5173",
            "https://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:3000",
            "https://localhost:3000"
        };

        var origins = allowedOrigins.Length > 0 ? allowedOrigins : defaultOrigins;

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<InnovationDashboardStore>();

var app = builder.Build();

app.UseCors();

var api = app.MapGroup("/api");

static bool TryValidateContext(string role, string? ministry, InnovationDashboardStore store, out UserContext context, out IResult? errorResult)
{
    context = UserContext.From(role, ministry);
    if (store.IsValidContext(context, out var error))
    {
        errorResult = null;
        return true;
    }

    errorResult = Results.BadRequest(new ApiErrorResponse("validation_error", error!));
    return false;
}

api.MapGet("/health", () => Results.Ok(new
{
    service = "Innovation4Albania Dashboard Backend",
    status = "ok",
    timestamp = DateTimeOffset.UtcNow
}));

api.MapGet("/reference-data/ministries", (InnovationDashboardStore store) => Results.Ok(store.GetMinistries()));

api.MapGet("/reference-data/roles", () =>
    Results.Ok(ApplicationRoles.All.Select(role => new { value = role, label = ApplicationRoles.ToDisplayLabel(role) })));

api.MapGet("/reference-data/statuses", () =>
    Results.Ok(ProjectStatuses.All.Select(status => new
    {
        value = status,
        label = ProjectStatuses.ToLabel(status),
        color = ProjectStatuses.ToColor(status)
    })));

api.MapPost("/auth/login", (LoginRequest request, InnovationDashboardStore store) =>
{
    var validationError = store.ValidateLogin(request);
    if (validationError is not null)
    {
        return Results.BadRequest(new ApiErrorResponse("validation_error", validationError));
    }

    return Results.Ok(store.Login(request));
});

api.MapGet("/dashboard/summary", (string role, string? ministry, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetDashboardSummary(context))
        : errorResult!;
});

api.MapGet("/dashboard/status-distribution", (string role, string? ministry, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetStatusDistribution(context))
        : errorResult!;
});

api.MapGet("/dashboard/performance", (string role, string? ministry, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetPerformanceScores(context))
        : errorResult!;
});

api.MapGet("/dashboard/trend", (int? months, InnovationDashboardStore store) =>
    Results.Ok(store.GetTrend(Math.Clamp(months.GetValueOrDefault(12), 3, 24))));

api.MapGet("/dashboard/ministry-distribution", (string role, string? ministry, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetMinistryDistribution(context))
        : errorResult!;
});

api.MapGet("/projects", (string role, string? ministry, string? status, string? query, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetProjects(context, status, query))
        : errorResult!;
});

api.MapGet("/projects/{id}", (string id, string role, string? ministry, InnovationDashboardStore store) =>
{
    if (!TryValidateContext(role, ministry, store, out var context, out var errorResult))
    {
        return errorResult!;
    }

    var project = store.GetProjectById(id, context);
    return project is null
        ? Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."))
        : Results.Ok(project);
});

api.MapPost("/projects", (string role, string? ministry, CreateProjectRequest request, InnovationDashboardStore store) =>
{
    if (!TryValidateContext(role, ministry, store, out var context, out var errorResult))
    {
        return errorResult!;
    }

    return store.TryCreateProject(context, request, out var project, out var error)
        ? Results.Ok(project)
        : Results.BadRequest(new ApiErrorResponse("project_create_failed", error!));
});

api.MapGet("/projects/{id}/events", (string id, string role, string? ministry, InnovationDashboardStore store) =>
{
    if (!TryValidateContext(role, ministry, store, out var context, out var errorResult))
    {
        return errorResult!;
    }

    if (store.GetProjectById(id, context) is null)
    {
        return Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."));
    }

    return Results.Ok(store.GetEventsForProject(id, context));
});

api.MapGet("/projects/{id}/ai-insights", (string id, string role, string? ministry, InnovationDashboardStore store) =>
{
    if (!TryValidateContext(role, ministry, store, out var context, out var errorResult))
    {
        return errorResult!;
    }

    var insights = store.GetProjectAiInsights(id, context);
    return insights is null
        ? Results.NotFound(new ApiErrorResponse("not_found", "Projekti nuk u gjet ose nuk është i aksesueshëm për këtë përdorues."))
        : Results.Ok(insights);
});

api.MapGet("/performance/board", (string role, string? ministry, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetPerformanceBoard(context))
        : errorResult!;
});

api.MapGet("/portfolio/okr", (string role, string? ministry, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetPortfolioOkr(context))
        : errorResult!;
});

api.MapPost("/portfolio/okr", (string role, string? ministry, CreatePortfolioObjectiveRequest request, InnovationDashboardStore store) =>
{
    if (!TryValidateContext(role, ministry, store, out var context, out var errorResult))
    {
        return errorResult!;
    }

    return store.TryCreatePortfolioObjective(context, request, out var objective, out var error)
        ? Results.Ok(objective)
        : Results.BadRequest(new ApiErrorResponse("portfolio_create_failed", error!));
});

api.MapGet("/risk-deviations", (string role, string? ministry, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetRiskDeviations(context))
        : errorResult!;
});

api.MapGet("/updates", (string role, string? ministry, string? projectId, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetWeeklyUpdates(context, projectId))
        : errorResult!;
});

api.MapPost("/updates", (string role, string? ministry, CreateWeeklyUpdateRequest request, InnovationDashboardStore store) =>
{
    if (!TryValidateContext(role, ministry, store, out var context, out var errorResult))
    {
        return errorResult!;
    }

    return store.TryCreateWeeklyUpdate(context, request, out var update, out var error)
        ? Results.Ok(update)
        : Results.BadRequest(new ApiErrorResponse("update_create_failed", error!));
});

api.MapGet("/change-proposals", (string role, string? ministry, string? projectId, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetChangeProposals(context, projectId))
        : errorResult!;
});

api.MapPost("/change-proposals", (string role, string? ministry, CreateProjectChangeProposalRequest request, InnovationDashboardStore store) =>
{
    if (!TryValidateContext(role, ministry, store, out var context, out var errorResult))
    {
        return errorResult!;
    }

    return store.TryCreateChangeProposal(context, request, out var proposal, out var error)
        ? Results.Ok(proposal)
        : Results.BadRequest(new ApiErrorResponse("change_proposal_failed", error!));
});

api.MapGet("/calendar/month", (string role, string? ministry, DateOnly? month, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetCalendarMonth(context, month ?? DateOnly.FromDateTime(DateTime.Today)))
        : errorResult!;
});

api.MapGet("/calendar/upcoming", (string role, string? ministry, int? limit, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetUpcomingEvents(context, Math.Clamp(limit.GetValueOrDefault(8), 1, 50)))
        : errorResult!;
});

api.MapPost("/ai/chat", (string role, string? ministry, AiChatRequest request, InnovationDashboardStore store) =>
{
    return TryValidateContext(role, ministry, store, out var context, out var errorResult)
        ? Results.Ok(store.GetAiChatReply(context, request))
        : errorResult!;
});

app.Run();
