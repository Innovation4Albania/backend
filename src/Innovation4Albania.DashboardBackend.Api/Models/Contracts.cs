namespace Innovation4Albania.DashboardBackend.Api.Models;

public sealed record LoginRequest(string Role, string? Ministry, string? Name, string? Username = null, string? Password = null, string? UserId = null, string? ProgramKey = null);

public sealed record UserContext(string Role, string? Ministry, string? Username = null, string? FullName = null, string? UserId = null, string? ProgramKey = null)
{
    public static UserContext From(string role, string? ministry, string? username = null, string? fullName = null, string? userId = null, string? programKey = null) =>
        new(
            role.Trim(),
            string.IsNullOrWhiteSpace(ministry) ? null : ministry.Trim(),
            string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
            string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
            string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            string.IsNullOrWhiteSpace(programKey) ? null : programKey.Trim());
}

public sealed record UserResponse(
    string Id,
    string Name,
    string Role,
    string? Ministry,
    string RoleLabel,
    string? ProgramKey = null);

public sealed record AuthResponse(string Token, UserResponse User);

public sealed record ManagedUserResponse(
    string Id,
    string Username,
    string Role,
    string? Ministry,
    string FullName,
    DateTimeOffset CreatedAt,
    bool IsActive);

public sealed record ViewUserResponse(
    string Id,
    string Role,
    string? Ministry,
    string FullName);

public sealed record CreateUserRequest(
    string FullName,
    string Username,
    string Password,
    string Role,
    string? Ministry = null);

public sealed record UpdateManagedUserRequest(
    string FullName,
    string Username,
    string? Password,
    string Role,
    string? Ministry = null);

public sealed record AdminResetPasswordRequest(string Password);

public sealed record ChangeOwnCredentialsRequest(
    string CurrentPassword,
    string? Username,
    string? NewPassword);

public sealed record ReferenceOptionResponse(string Value, string Label);

public sealed record StatusReferenceResponse(string Value, string Label, string Color);

public sealed record ProjectOkr(
    int Deadlines,
    int Quality,
    int Impact,
    int Dynamics);

public sealed record KeyResultResponse(
    string Id,
    string Title,
    int Progress,
    int Target,
    string Unit,
    string MeasurementType = "manual",
    int CurrentValue = 0,
    bool IsAutomatic = false);

public sealed record ObjectiveResponse(
    string Id,
    string Title,
    string Owner,
    int Progress,
    IReadOnlyList<KeyResultResponse> KeyResults);

public sealed record WorkgroupMemberResponse(
    string Id,
    string Name,
    string Role,
    string RoleLabel,
    string Unit,
    int AllocationPercent,
    string? UserId = null,
    string? AccountRole = null);

public sealed record ResourceCapacitySummaryResponse(
    int TotalPeople,
    int AverageTeamSize,
    int AverageCapacityUtilization,
    int LeadershipRoles,
    int CrossInstitutionProjects,
    IReadOnlyList<ResourceUnitAllocationResponse> UnitAllocations);

public sealed record ResourceUnitAllocationResponse(
    string Unit,
    int People,
    int CapacityPercent);

public sealed record ProjectResponse(
    string Id,
    string Code,
    string Name,
    string Description,
    string? ProgramKey,
    IReadOnlyList<string> Ministries,
    string? Agency,
    IReadOnlyList<string> Directorates,
    string Status,
    string Priority,
    string PriorityLabel,
    string Sector,
    string SectorLabel,
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
    IReadOnlyList<WorkgroupMemberResponse> TeamMembers,
    string Lead,
    int UpdateCadenceDays,
    DateTimeOffset LastUpdated,
    int OkrAverage,
    bool IsOverdue,
    int TotalCapacityPercent,
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

public sealed record UpcomingEventResponse(
    string Id,
    string ProjectId,
    DateTimeOffset Date,
    string Type,
    string TypeLabel,
    string Title,
    string ProjectCode,
    string ProjectName);

public sealed record AiInsightResponse(
    string ProjectId,
    string AttentionLevel,
    string Summary,
    string RiskExplanation,
    int RiskScore,
    string RiskPrediction,
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
    string ExpertName,
    DateTimeOffset SubmittedAt,
    int Progress,
    string Status,
    int OkrAverage,
    string Risk,
    string Blockers,
    string Comments,
    IReadOnlyList<WeeklyUpdateKeyResultInput> KeyResults);

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
    string Status,
    string? ResolutionReason = null);

public sealed record ChatMessageResponse(
    string Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record AiChatResponse(
    ChatMessageResponse Reply,
    IReadOnlyList<string> SuggestedActions);

public sealed record AiChatMessageRequest(
    string Role,
    string Content);

public sealed record ObjectiveInput(
    string Title,
    string Owner,
    IReadOnlyList<KeyResultInput> KeyResults);

public sealed record KeyResultInput(
    string Title,
    int Progress,
    int Target,
    string Unit,
    string? MeasurementType = null);

public sealed record WorkgroupMemberInput(
    string Name,
    string Role,
    string Unit,
    int AllocationPercent,
    string? UserId = null,
    string? AccountRole = null);

public sealed record CreateProjectRequest(
    string Code,
    string Name,
    string Description,
    string? ProgramKey,
    IReadOnlyList<string> Ministries,
    string? Agency,
    IReadOnlyList<string>? Directorates,
    string Status,
    string Priority,
    string Sector,
    int TotalPhases,
    int CurrentPhase,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int Progress,
    string Risk,
    IReadOnlyList<string> Team,
    IReadOnlyList<WorkgroupMemberInput> TeamMembers,
    string Lead,
    int UpdateCadenceDays,
    IReadOnlyList<ObjectiveInput> Objectives);

public sealed record CreatePortfolioObjectiveRequest(
    string Title,
    string Owner,
    IReadOnlyList<KeyResultInput> KeyResults);

public sealed record CreateWeeklyUpdateRequest(
    string ProjectId,
    string? ExpertName,
    int Progress,
    string Status,
    string Risk,
    string Blockers,
    string Comments,
    IReadOnlyList<WeeklyUpdateKeyResultInput>? KeyResults = null);

public sealed record WeeklyUpdateKeyResultInput(
    string KeyResultId,
    int CurrentValue);

public sealed record CreateProjectChangeProposalRequest(
    string ProjectId,
    string Type,
    string CurrentValue,
    string ProposedValue,
    string Reason);

public sealed record ResolveChangeProposalRequest(string Action, string? ResolutionReason = null);

public sealed record AiChatRequest(
    string Message,
    IReadOnlyList<AiChatMessageRequest>? History = null);

public sealed record ApiErrorResponse(string Code, string Message);

