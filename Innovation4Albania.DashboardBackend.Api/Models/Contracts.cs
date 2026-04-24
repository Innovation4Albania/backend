namespace Innovation4Albania.DashboardBackend.Api.Models;

public sealed record LoginRequest(string Role, string? Ministry, string? Name);

public sealed record UserContext(string Role, string? Ministry)
{
    public static UserContext From(string role, string? ministry) =>
        new(role.Trim(), string.IsNullOrWhiteSpace(ministry) ? null : ministry.Trim());
}

public sealed record UserResponse(
    string Id,
    string Name,
    string Role,
    string? Ministry,
    string RoleLabel);

public sealed record ProjectOkr(
    int Deadlines,
    int Quality,
    int Impact,
    int Collaboration);

public sealed record KeyResultResponse(
    string Id,
    string Title,
    int Progress,
    int Target,
    string Unit);

public sealed record ObjectiveResponse(
    string Id,
    string Title,
    string Owner,
    int Progress,
    IReadOnlyList<KeyResultResponse> KeyResults);

public sealed record ProjectResponse(
    string Id,
    string Code,
    string Name,
    string Description,
    IReadOnlyList<string> Ministries,
    string? Agency,
    string Status,
    int TotalPhases,
    int CurrentPhase,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int Progress,
    int ExpectedProgress,
    int DeviationPercent,
    int DaysRemaining,
    int DelayDays,
    ProjectOkr Okr,
    string Risk,
    IReadOnlyList<string> Team,
    string Lead,
    DateTimeOffset LastUpdated,
    int UpdateCadenceDays,
    int OkrAverage,
    bool IsOverdue,
    IReadOnlyList<ObjectiveResponse> Objectives);

public sealed record ProjectEventResponse(
    string Id,
    string ProjectId,
    DateTimeOffset Date,
    string Type,
    string Title);

public sealed record StatusCardResponse(
    string Key,
    string Label,
    int Value,
    string Color);

public sealed record PortfolioMetricsResponse(
    int AverageOkr,
    int OnTimePercentage,
    int DeviationAverage,
    int ProjectsNeedingAttention);

public sealed record DashboardSummaryResponse(
    int TotalProjects,
    IReadOnlyList<StatusCardResponse> StatusCards,
    PortfolioMetricsResponse Portfolio);

public sealed record StatusDistributionItem(string Status, string Label, int Value, string Color);

public sealed record MinistryDistributionItem(string Ministry, int Value, string Color);

public sealed record PerformanceScoreItem(
    string ProjectId,
    string Code,
    string Name,
    int Score,
    int Progress,
    string Risk);

public sealed record TrendPointResponse(string Label, int Progress, int Okr);

public sealed record PerformanceBoardColumnResponse(
    string Key,
    string Label,
    string Hint,
    IReadOnlyList<PerformanceScoreItem> Items);

public sealed record CalendarDayEventResponse(
    string EventId,
    string ProjectId,
    string ProjectCode,
    string ProjectName,
    string Type,
    string TypeLabel,
    string Title);

public sealed record CalendarDayResponse(
    DateOnly Date,
    bool IsCurrentMonth,
    bool IsToday,
    IReadOnlyList<CalendarDayEventResponse> Events);

public sealed record CalendarMonthResponse(
    DateOnly CursorMonth,
    DateOnly GridStart,
    DateOnly GridEnd,
    IReadOnlyList<CalendarDayResponse> Days);

public sealed record AiInsightResponse(
    string ProjectId,
    string AttentionLevel,
    string Summary,
    string RiskExplanation,
    int ConfidenceScore,
    IReadOnlyList<string> PositiveSignals,
    IReadOnlyList<string> Concerns,
    IReadOnlyList<string> Recommendations);

public sealed record PortfolioOkrResponse(
    PortfolioMetricsResponse Metrics,
    IReadOnlyList<ObjectiveResponse> Objectives);

public sealed record RiskDeviationResponse(
    string ProjectId,
    string ProjectCode,
    string ProjectName,
    string Status,
    string Risk,
    int CurrentProgress,
    int ExpectedProgress,
    int DeviationPercent,
    int DaysRemaining,
    int DelayDays,
    string Urgency);

public sealed record WeeklyUpdateResponse(
    string Id,
    string ProjectId,
    string ProjectCode,
    string ProjectName,
    string SubmittedBy,
    string SubmittedRole,
    DateTimeOffset SubmittedAt,
    int Progress,
    string Status,
    int OkrAverage,
    string Risk,
    string Blockers,
    string Comments);

public sealed record ProjectChangeProposalResponse(
    string Id,
    string ProjectId,
    string ProjectCode,
    string ProjectName,
    string SubmittedBy,
    string SubmittedRole,
    DateTimeOffset SubmittedAt,
    string Type,
    string TypeLabel,
    string CurrentValue,
    string ProposedValue,
    string Reason,
    string Status);

public sealed record ChatMessageResponse(
    string Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record AiChatResponse(
    ChatMessageResponse Reply,
    IReadOnlyList<string> SuggestedActions);

public sealed record ObjectiveInput(
    string Title,
    string Owner,
    IReadOnlyList<KeyResultInput> KeyResults);

public sealed record KeyResultInput(
    string Title,
    int Progress,
    int Target,
    string Unit);

public sealed record CreateProjectRequest(
    string Code,
    string Name,
    string Description,
    IReadOnlyList<string> Ministries,
    string? Agency,
    string Status,
    int TotalPhases,
    int CurrentPhase,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int Progress,
    ProjectOkr Okr,
    string Risk,
    IReadOnlyList<string> Team,
    string Lead,
    int UpdateCadenceDays,
    IReadOnlyList<ObjectiveInput> Objectives);

public sealed record CreatePortfolioObjectiveRequest(
    string Title,
    string Owner,
    IReadOnlyList<KeyResultInput> KeyResults);

public sealed record CreateWeeklyUpdateRequest(
    string ProjectId,
    int Progress,
    string Status,
    ProjectOkr Okr,
    string Risk,
    string Blockers,
    string Comments);

public sealed record CreateProjectChangeProposalRequest(
    string ProjectId,
    string Type,
    string CurrentValue,
    string ProposedValue,
    string Reason);

public sealed record AiChatRequest(string Message);

public sealed record ApiErrorResponse(string Code, string Message);
