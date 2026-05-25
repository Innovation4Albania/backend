using System.Globalization;
using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Configuration;
using Innovation4Albania.DashboardBackend.Api.Models;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Innovation4Albania.DashboardBackend.Api.Data;

public sealed class InnovationDashboardStore
{
    private static readonly CultureInfo AlbanianCulture = CultureInfo.GetCultureInfo("sq-AL");
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyList<string> _ministries =
    [
        "Ministria e Infrastrukturës dhe Energjisë",
        "Ministria e Punëve të Brendshme",
        "Ministria për Evropën dhe Punët e Jashtme",
        "Ministria e Financave",
        "Ministria e Kulturës dhe Turizmit",
        "Ministria e Mjedisit",
        "Ministria e Shëndetësisë dhe Mirëqenies Sociale",
        "Ministria e Ekonomisë dhe Inovacionit",
        "Ministria e Drejtësisë",
        "Ministria e Mbrojtjes",
        "Ministria e Bujqësisë dhe Zhvillimit Rural",
        "Ministria e Shtetit për Pushtetin Vendor",
        "Ministria e Shtetit për Administratën Publike dhe Antikorrupsionin",
        "Ministria për Marrëdhëniet me Parlamentin"
    ];

    private readonly List<ProjectState> _projects;
    private readonly List<ObjectiveState> _portfolioObjectives;
    private readonly List<WeeklyUpdateState> _updates;
    private readonly List<ProjectChangeProposalState> _changeProposals;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InnovationDashboardStore> _logger;
    private readonly IDashboardStorePersistence _persistence;
    private readonly IOptions<GeminiOptions> _geminiOptions;
    private readonly SemaphoreSlim _mutationLock = new(1, 1);
    private readonly SemaphoreSlim _persistenceLock = new(1, 1);

    public InnovationDashboardStore(
        IHttpClientFactory httpClientFactory,
        ILogger<InnovationDashboardStore> logger,
        IDashboardStorePersistence persistence,
        IOptions<GeminiOptions> geminiOptions)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _persistence = persistence;
        _geminiOptions = geminiOptions;
        _projects = BuildProjects();
        _portfolioObjectives = BuildPortfolioObjectives();
        _updates = BuildUpdates();
        _changeProposals = [];
        RecalculateAllProjectOkrs();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_persistence.IsConfigured)
        {
            _logger.LogInformation("PostgreSQL persistence is not configured. Using in-memory dashboard seed data.");
            return;
        }

        try
        {
            var payload = await _persistence.LoadSnapshotAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                var seedPayload = await ExecuteReadAsync(() => JsonSerializer.Serialize(BuildSnapshot(), SnapshotJsonOptions));
                await SaveSnapshotAsync(seedPayload, cancellationToken);
                return;
            }

            var snapshot = JsonSerializer.Deserialize<DashboardStoreSnapshot>(payload, SnapshotJsonOptions);
            if (snapshot is not null)
            {
                await ExecuteMutationAsync(() =>
                {
                    RestoreSnapshot(snapshot);
                    return Task.FromResult(true);
                });
                _logger.LogInformation("Dashboard state loaded from PostgreSQL.");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Dashboard state could not be loaded from PostgreSQL. Falling back to seed data.");
        }
    }

    private async Task<bool> PersistSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (!_persistence.IsConfigured)
        {
            return true;
        }

        try
        {
            var payload = JsonSerializer.Serialize(BuildSnapshot(), SnapshotJsonOptions);
            return await SaveSnapshotAsync(payload, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Dashboard state could not be serialized for PostgreSQL.");
            return false;
        }
    }

    private async Task<bool> SaveSnapshotAsync(string payload, CancellationToken cancellationToken = default)
    {
        if (!_persistence.IsConfigured)
        {
            return true;
        }

        var lockTaken = false;
        try
        {
            await _persistenceLock.WaitAsync(cancellationToken);
            lockTaken = true;
            await _persistence.SaveSnapshotAsync(payload, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Dashboard state could not be saved to PostgreSQL.");
            return false;
        }
        finally
        {
            if (lockTaken)
            {
                _persistenceLock.Release();
            }
        }
    }

    private async Task<T> ExecuteMutationAsync<T>(Func<Task<T>> mutation)
    {
        await _mutationLock.WaitAsync();
        try
        {
            return await mutation();
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private async Task<T> ExecuteReadAsync<T>(Func<T> read)
    {
        await _mutationLock.WaitAsync();
        try
        {
            return read();
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private DashboardStoreSnapshot BuildSnapshot() =>
        new(
            _projects.Select(ToProjectSnapshot).ToList(),
            _portfolioObjectives.Select(ToObjectiveResponse).ToList(),
            _updates.Select(update => new WeeklyUpdateSnapshot(
                update.Id,
                update.ProjectId,
                update.SubmittedBy,
                update.SubmittedRole,
                update.SubmittedAt,
                update.Progress,
                update.Status,
                update.Risk,
                update.Blockers,
                update.Comments,
                update.ExpertName,
                update.KeyResults.Select(kr => new WeeklyUpdateKeyResultInput(kr.KeyResultId, kr.CurrentValue)).ToList())).ToList(),
            _changeProposals.Select(proposal => new ProjectChangeProposalSnapshot(
                proposal.Id,
                proposal.ProjectId,
                proposal.SubmittedBy,
                proposal.SubmittedRole,
                proposal.SubmittedAt,
                proposal.Type,
                proposal.CurrentValue,
                proposal.ProposedValue,
                proposal.Reason,
            NormalizeProposalStatus(proposal.Status))).ToList());

    private void RestoreSnapshot(DashboardStoreSnapshot snapshot)
    {
        var projectIdsMissingOkr = snapshot.Projects
            .Where(project => project.Okr is null)
            .Select(project => project.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _projects.Clear();
        _projects.AddRange(snapshot.Projects.Select(ToProjectState));

        _portfolioObjectives.Clear();
        _portfolioObjectives.AddRange(snapshot.PortfolioObjectives.Select(ToObjectiveState));

        _updates.Clear();
        _updates.AddRange(snapshot.Updates.Select(update => new WeeklyUpdateState(
            update.Id,
            update.ProjectId,
            ApplicationRoles.ToDisplayLabel(update.SubmittedRole),
            update.SubmittedRole,
            string.IsNullOrWhiteSpace(update.ExpertName) ? update.SubmittedBy : update.ExpertName,
            update.SubmittedAt,
            update.Progress,
            update.Status,
            update.Risk,
            update.Blockers,
            update.Comments,
            update.KeyResults?.Select(kr => new WeeklyUpdateKeyResultState(kr.KeyResultId, kr.CurrentValue)).ToList() ?? [])));

        _changeProposals.Clear();
        _changeProposals.AddRange(snapshot.ChangeProposals.Select(proposal => new ProjectChangeProposalState(
            proposal.Id,
            proposal.ProjectId,
            proposal.SubmittedBy,
            proposal.SubmittedRole,
            proposal.SubmittedAt,
            proposal.Type,
            proposal.CurrentValue,
            proposal.ProposedValue,
            proposal.Reason,
            NormalizeProposalStatus(proposal.Status))));

        if (projectIdsMissingOkr.Count > 0)
        {
            var updatesByProject = BuildUpdatesByProjectLookup();
            foreach (var project in _projects.Where(project => projectIdsMissingOkr.Contains(project.Id)))
            {
                RecalculateProjectOkr(project, updatesByProject);
            }
        }
    }

    public IReadOnlyList<string> GetMinistries() => _ministries;

    public string? ValidateLogin(LoginRequest request)
    {
        var context = UserContext.From(request.Role, request.Ministry);
        return IsValidContext(context, out var error) ? null : error;
    }

    public bool IsValidContext(UserContext context, out string? error)
    {
        if (!ApplicationRoles.All.Contains(context.Role))
        {
            error = "Roli nuk është i vlefshëm.";
            return false;
        }

        if (ApplicationRoles.RequiresMinistry(context.Role))
        {
            if (string.IsNullOrWhiteSpace(context.Ministry))
            {
                error = "Ky rol kërkon zgjedhjen e një ministrie.";
                return false;
            }

            if (ResolveMinistry(context.Ministry) is null)
            {
                error = "Ministria nuk është e vlefshme.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public UserResponse Login(LoginRequest request)
    {
        var context = UserContext.From(request.Role, request.Ministry);
        var canonicalMinistry = ResolveMinistry(context.Ministry) ?? context.Ministry;
        var displayName = string.IsNullOrWhiteSpace(request.Name)
            ? ApplicationRoles.ToDisplayLabel(context.Role)
            : request.Name.Trim();

        return new UserResponse(
            $"{context.Role}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            displayName,
            context.Role,
            canonicalMinistry,
            ApplicationRoles.ToDisplayLabel(context.Role));
    }

    public Task<DashboardSummaryResponse> GetDashboardSummary(UserContext context) => ExecuteReadAsync(() =>
    {
        var visible = GetVisibleProjects(context);
        var statusCards = ProjectStatuses.All
            .Select(status => new StatusCardResponse(
                status,
                ProjectStatuses.ToLabel(status),
                visible.Count(project => project.Status == status),
                ProjectStatuses.ToColor(status)))
            .ToList();

        return new DashboardSummaryResponse(visible.Count, statusCards, BuildPortfolioMetrics(visible));
    });

    public Task<IReadOnlyList<StatusDistributionItem>> GetStatusDistribution(UserContext context) => ExecuteReadAsync<IReadOnlyList<StatusDistributionItem>>(() =>
    {
        var visible = GetVisibleProjects(context);

        return ProjectStatuses.All
            .Select(status => new StatusDistributionItem(
                status,
                ProjectStatuses.ToLabel(status),
                visible.Count(project => project.Status == status),
                ProjectStatuses.ToColor(status)))
            .Where(item => item.Value > 0)
            .ToList();
    });

    public Task<IReadOnlyList<MinistryDistributionItem>> GetMinistryDistribution(UserContext context) => ExecuteReadAsync<IReadOnlyList<MinistryDistributionItem>>(() =>
    {
        var palette = new[]
        {
            "hsl(var(--primary))",
            "hsl(var(--accent))",
            "hsl(var(--info))",
            "hsl(var(--warning))",
            "hsl(var(--success))",
            "hsl(var(--destructive))"
        };

        var visible = GetVisibleProjects(context);

        var visibleMinistries = context.Role == ApplicationRoles.StafMinistrie
            ? ResolveMinistry(context.Ministry) is { } ministry ? [ministry] : []
            : _ministries
                .Where(ministry => visible.Any(project => project.Ministries.Contains(ministry)))
                .ToList();

        return visibleMinistries
            .Select((ministry, index) => new MinistryDistributionItem(
                ShortMinistryName(ministry),
                visible.Count(project => project.Ministries.Contains(ministry)),
                palette[index % palette.Length]))
            .ToList();
    });

    public Task<ResourceCapacitySummaryResponse> GetResourceCapacitySummary(UserContext context) => ExecuteReadAsync(() =>
    {
        var visible = GetVisibleProjects(context);
        if (visible.Count == 0)
        {
            return new ResourceCapacitySummaryResponse(0, 0, 0, 0, 0, []);
        }

        var members = visible.SelectMany(project => project.TeamMembers).ToList();
        var totalPeople = members
            .Select(member => member.Name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var averageTeamSize = (int)Math.Round(visible.Average(project => project.TeamMembers.Count));
        var averageCapacity = (int)Math.Round(visible.Average(project => project.TotalCapacityPercent));
        var leadershipRoles = members.Count(member => member.Role is WorkgroupRoles.ProjectLead or WorkgroupRoles.OkrOwner);
        var crossInstitutionProjects = visible.Count(project => project.Ministries.Count > 1);

        var unitAllocations = members
            .GroupBy(member => string.IsNullOrWhiteSpace(member.Unit) ? "Njësi e pacaktuar" : member.Unit.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ResourceUnitAllocationResponse(
                group.Key,
                group.Select(member => member.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                group.Sum(member => member.AllocationPercent)))
            .OrderByDescending(item => item.CapacityPercent)
            .ThenBy(item => item.Unit)
            .ToList();

        return new ResourceCapacitySummaryResponse(
            totalPeople,
            averageTeamSize,
            averageCapacity,
            leadershipRoles,
            crossInstitutionProjects,
            unitAllocations);
    });

    public Task<IReadOnlyList<PerformanceScoreItem>> GetPerformanceScores(UserContext context) =>
        ExecuteReadAsync<IReadOnlyList<PerformanceScoreItem>>(() =>
        {
            return GetVisibleProjects(context)
                .Select(project => new PerformanceScoreItem(project.Id, project.Code, project.Name, GetOkrAverage(project), project.Progress, project.Risk))
                .OrderByDescending(item => item.Score)
                .ToList();
        });

    public IReadOnlyList<TrendPointResponse> GetTrend(int months)
    {
        var start = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(months - 1));
        var points = new List<TrendPointResponse>(months);

        for (var i = 0; i < months; i++)
        {
            var month = start.AddMonths(i);
            var progress = (int)Math.Round(32 + (i * 4.1) + (Math.Sin(i / 2d) * 7));
            var okr = (int)Math.Round(58 + (i * 1.8) + (Math.Cos(i / 2d) * 4));
            points.Add(new TrendPointResponse(month.ToString("MMM", AlbanianCulture), Math.Clamp(progress, 0, 100), Math.Clamp(okr, 0, 100)));
        }

        return points;
    }

    public Task<IReadOnlyList<ProjectResponse>> GetProjects(UserContext context, string? status, string? query) => ExecuteReadAsync<IReadOnlyList<ProjectResponse>>(() =>
    {
        var visible = GetVisibleProjects(context);

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            visible = visible.Where(project => project.Status == status.Trim()).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            visible = visible.Where(project =>
                    project.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    project.Code.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    project.Ministries.Any(ministry => ministry.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                    ProjectPriorities.ToLabel(project.Priority).Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    ProjectSectors.ToLabel(project.Sector).Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    project.TeamMembers.Any(member =>
                        member.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                        WorkgroupRoles.ToLabel(member.Role).Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                        member.Unit.Contains(normalized, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return visible
            .OrderByDescending(project => project.DelayDays > 0)
            .ThenByDescending(project => project.DeviationPercent)
            .ThenBy(project => project.Name)
            .Select(ToResponse)
            .ToList();
    });

    public Task<ProjectResponse?> GetProjectById(string id, UserContext context) =>
        ExecuteReadAsync(() =>
        {
            var project = GetVisibleProjects(context)
                .FirstOrDefault(project => string.Equals(project.Id, id, StringComparison.OrdinalIgnoreCase));

            return project is null ? null : ToResponse(project);
        });

    public async Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryCreateProjectAsync(UserContext context, CreateProjectRequest request)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, ProjectResponse? Response, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanCreateProjects(context.Role))
            {
                return (false, null, "Vetem Drejtori i Agjencise dhe Drejtori i Inovacionit Publik mund te krijojne projekte.");
            }

            if (!TryValidateProjectRequest(request, out var error))
            {
                return (false, null, error);
            }

            if (HasProjectWithCode(request.Code))
            {
                return (false, null, "Ekziston tashme nje projekt me kete kod.");
            }

            var projectNumber = GetNextProjectNumber();
            var now = DateTimeOffset.UtcNow;
            var teamMembers = BuildTeamMembersForRequest(projectNumber, request);
            var progress = Math.Clamp(request.Progress, 0, 100);
            var project = new ProjectState(
                $"p{projectNumber}",
                request.Code.Trim(),
                request.Name.Trim(),
                request.Description.Trim(),
                request.Ministries.Count == 0 ? ["—"] : request.Ministries.Select(item => item.Trim()).ToList(),
                string.IsNullOrWhiteSpace(request.Agency) ? null : request.Agency.Trim(),
                ResolveStatusForProgress(request.Status, progress),
                request.Priority,
                request.Sector,
                request.TotalPhases <= 0 ? 6 : Math.Max(1, request.TotalPhases),
                1,
                request.StartDate,
                request.EndDate,
                progress,
                NeutralOkr(),
                request.Risk,
                teamMembers.Select(member => member.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                teamMembers,
                request.Lead.Trim(),
                14,
                now,
                request.Objectives.Select((objective, index) => ToObjectiveState($"obj-{projectNumber}-{index + 1}", objective)).ToList());

            _projects.Add(project);
            RecalculateProjectOkr(project, BuildUpdatesByProjectLookup());
            var response = ToResponse(project);
            return await PersistSnapshotAsync()
                ? (true, response, null)
                : (false, null, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }
    public async Task<(bool IsSuccess, ProjectResponse? Response, string? Error)> TryUpdateProjectAsync(UserContext context, string id, CreateProjectRequest request)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, ProjectResponse? Response, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanCreateProjects(context.Role))
            {
                return (false, null, "Vetem Drejtori i Agjencise dhe Drejtori i Inovacionit Publik mund te editojne projekte.");
            }

            var project = GetVisibleProjects(context)
                .FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

            if (project is null)
            {
                return (false, null, "Projekti nuk u gjet.");
            }

            if (!TryValidateProjectRequest(request, out var error))
            {
                return (false, null, error);
            }

            if (HasProjectWithCode(request.Code, project.Id))
            {
                return (false, null, "Ekziston tashme nje projekt me kete kod.");
            }

            ApplyRequestToProjectState(project, request);
            project.LastUpdated = DateTimeOffset.UtcNow;
            RecalculateProjectOkr(project, BuildUpdatesByProjectLookup());

            var response = ToResponse(project);
            return await PersistSnapshotAsync()
                ? (true, response, null)
                : (false, null, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    public async Task<(bool IsSuccess, string? Error)> TryDeleteProjectAsync(UserContext context, string id)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanCreateProjects(context.Role))
            {
                return (false, "Vetem Drejtori i Agjencise dhe Drejtori i Inovacionit Publik mund te fshijne projekte.");
            }

            var project = GetVisibleProjects(context)
                .FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

            if (project is null)
            {
                return (false, "Projekti nuk u gjet.");
            }

            _updates.RemoveAll(update => string.Equals(update.ProjectId, project.Id, StringComparison.OrdinalIgnoreCase));
            _changeProposals.RemoveAll(proposal => string.Equals(proposal.ProjectId, project.Id, StringComparison.OrdinalIgnoreCase));
            _projects.Remove(project);

            return await PersistSnapshotAsync()
                ? (true, null)
                : (false, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }
    private static bool TryValidateProjectRequest(CreateProjectRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            error = "Kodi dhe emri i projektit janë të detyrueshëm.";
            return false;
        }

        if (!ProjectStatuses.All.Contains(request.Status))
        {
            error = "Statusi i zgjedhur nuk është i vlefshëm.";
            return false;
        }

        if (!ProjectPriorities.All.Contains(request.Priority))
        {
            error = "Prioriteti i zgjedhur nuk është i vlefshëm.";
            return false;
        }

        if (!ProjectSectors.All.Contains(request.Sector))
        {
            error = "Sektori i zgjedhur nuk është i vlefshëm.";
            return false;
        }

        if (!RiskLevels.All.Contains(request.Risk))
        {
            error = "Niveli i riskut nuk është i vlefshëm.";
            return false;
        }

        if (request.EndDate < request.StartDate)
        {
            error = "Data e mbylljes nuk mund të jetë më e hershme se data e nisjes.";
            return false;
        }

        error = null;
        return true;
    }

    private bool HasProjectWithCode(string code, string? excludedProjectId = null) =>
        _projects.Any(project =>
            !string.Equals(project.Id, excludedProjectId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(project.Code.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase));

    private void ApplyRequestToProjectState(ProjectState project, CreateProjectRequest request)
    {
        var projectNumber = ParseProjectNumber(project.Id);
        var teamMembers = BuildTeamMembersForRequest(projectNumber, request);

        project.Code = request.Code.Trim();
        project.Name = request.Name.Trim();
        project.Description = request.Description.Trim();
        project.Agency = string.IsNullOrWhiteSpace(request.Agency) ? null : request.Agency.Trim();
        project.Priority = request.Priority;
        project.Sector = request.Sector;
        project.TotalPhases = request.TotalPhases <= 0 ? 6 : Math.Max(1, request.TotalPhases);
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Progress = Math.Clamp(request.Progress, 0, 100);
        project.Status = ResolveStatusForProgress(request.Status, project.Progress);
        project.CurrentPhase = CalculateCurrentPhase(project.Progress, project.TotalPhases);
        project.Risk = request.Risk;
        project.Lead = request.Lead.Trim();
        project.UpdateCadenceDays = 14;

        project.Ministries.Clear();
        project.Ministries.AddRange(request.Ministries.Count == 0
            ? ["—"]
            : request.Ministries.Select(item => item.Trim()).Where(item => item.Length > 0));

        project.Team.Clear();
        project.Team.AddRange(teamMembers.Select(member => member.Name).Distinct(StringComparer.OrdinalIgnoreCase));

        project.TeamMembers.Clear();
        project.TeamMembers.AddRange(teamMembers);

        project.Objectives.Clear();
        project.Objectives.AddRange(request.Objectives.Select((objective, index) => ToObjectiveState($"obj-{projectNumber}-{index + 1}", objective)));
    }

    private static int ParseProjectNumber(string projectId)
    {
        var digits = new string(projectId.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) && number > 0 ? number : 1;
    }

    private int GetNextProjectNumber() =>
        _projects.Count == 0 ? 1 : _projects.Max(project => ParseProjectNumber(project.Id)) + 1;

    private static int ParsePrefixedNumber(string id, string prefix)
    {
        var suffix = id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? id[prefix.Length..]
            : id;

        return int.TryParse(suffix, out var number) && number > 0 ? number : 0;
    }

    private int GetNextWeeklyUpdateNumber() =>
        _updates.Count == 0
            ? 1
            : Math.Max(_updates.Count, _updates.Max(update => ParsePrefixedNumber(update.Id, "upd-"))) + 1;

    private int GetNextChangeProposalNumber() =>
        _changeProposals.Count == 0
            ? 1
            : Math.Max(_changeProposals.Count, _changeProposals.Max(proposal => ParsePrefixedNumber(proposal.Id, "chg-"))) + 1;

    public Task<IReadOnlyList<ProjectEventResponse>> GetEventsForProject(string projectId, UserContext context) => ExecuteReadAsync(() =>
    {
        var project = GetVisibleProjects(context).FirstOrDefault(item => item.Id == projectId);
        return project is null ? [] : BuildProjectEvents(project);
    });

    public async Task<AiInsightResponse?> GetProjectAiInsights(string projectId, UserContext context)
    {
        var insightState = await ExecuteReadAsync(() =>
        {
            var project = GetVisibleProjects(context).FirstOrDefault(item => item.Id == projectId);
            if (project is null)
            {
                return null;
            }

            var response = ToResponse(project);

            return new AiInsightState(ToProjectState(ToProjectSnapshot(project)), response);
        });

        return insightState is null
            ? null
            : await BuildAiInsights(insightState.Project, insightState.Response);
    }

    public Task<IReadOnlyList<PerformanceBoardColumnResponse>> GetPerformanceBoard(UserContext context) => ExecuteReadAsync<IReadOnlyList<PerformanceBoardColumnResponse>>(() =>
    {
        var scored = GetVisibleProjects(context)
            .Select(project => new PerformanceScoreItem(project.Id, project.Code, project.Name, GetOkrAverage(project), project.Progress, project.Risk))
            .ToList();

        return
        [
            new(
                PerformanceBuckets.Excellent,
                PerformanceBuckets.ToLabel(PerformanceBuckets.Excellent),
                ">= 85",
                scored.Where(item => GetPerformanceBucket(item.Score) == PerformanceBuckets.Excellent).OrderByDescending(item => item.Score).ToList()),
            new(
                PerformanceBuckets.Good,
                PerformanceBuckets.ToLabel(PerformanceBuckets.Good),
                "70 - 84",
                scored.Where(item => GetPerformanceBucket(item.Score) == PerformanceBuckets.Good).OrderByDescending(item => item.Score).ToList()),
            new(
                PerformanceBuckets.NeedsAttention,
                PerformanceBuckets.ToLabel(PerformanceBuckets.NeedsAttention),
                "55 - 69",
                scored.Where(item => GetPerformanceBucket(item.Score) == PerformanceBuckets.NeedsAttention).OrderByDescending(item => item.Score).ToList()),
            new(
                PerformanceBuckets.Completed,
                PerformanceBuckets.ToLabel(PerformanceBuckets.Completed),
                "Statusi: Përfunduara",
                GetVisibleProjects(context)
                    .Where(project => project.Status == ProjectStatuses.Completed)
                    .Select(project => new PerformanceScoreItem(project.Id, project.Code, project.Name, GetOkrAverage(project), project.Progress, project.Risk))
                    .OrderByDescending(item => item.Score)
                    .ToList())
        ];
    });

    public Task<PortfolioOkrResponse> GetPortfolioOkr(UserContext context) => ExecuteReadAsync(() =>
    {
        var visible = GetVisibleProjects(context);
        var objectives = _portfolioObjectives.Select(ToObjectiveResponse).ToList();
        return new PortfolioOkrResponse(BuildPortfolioMetrics(visible), objectives);
    });

    public async Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryCreatePortfolioObjectiveAsync(UserContext context, CreatePortfolioObjectiveRequest request)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, ObjectiveResponse? Response, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanManagePortfolio(context.Role))
            {
                return (false, null, "Vetem Drejtori i Inovacionit mund te shtoje OKR te portofolit.");
            }

            if (!TryValidatePortfolioObjectiveRequest(request, out var error))
            {
                return (false, null, error);
            }

            var state = ToObjectiveState($"portfolio-{GetNextPortfolioObjectiveNumber()}", new ObjectiveInput(request.Title, request.Owner, request.KeyResults));
            _portfolioObjectives.Add(state);
            var response = ToObjectiveResponse(state);
            return await PersistSnapshotAsync()
                ? (true, response, null)
                : (false, null, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    public async Task<(bool IsSuccess, ObjectiveResponse? Response, string? Error)> TryUpdatePortfolioObjectiveAsync(UserContext context, string id, CreatePortfolioObjectiveRequest request)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, ObjectiveResponse? Response, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanManagePortfolio(context.Role))
            {
                return (false, null, "Vetem Drejtori i Inovacionit mund te editoje OKR te portofolit.");
            }

            if (!TryValidatePortfolioObjectiveRequest(request, out var error))
            {
                return (false, null, error);
            }

            var index = _portfolioObjectives.FindIndex(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return (false, null, "Objektivi nuk u gjet.");
            }

            var state = ToObjectiveState(_portfolioObjectives[index].Id, new ObjectiveInput(request.Title, request.Owner, request.KeyResults));
            _portfolioObjectives[index] = state;
            var response = ToObjectiveResponse(state);
            return await PersistSnapshotAsync()
                ? (true, response, null)
                : (false, null, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    public async Task<(bool IsSuccess, string? Error)> TryDeletePortfolioObjectiveAsync(UserContext context, string id)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanManagePortfolio(context.Role))
            {
                return (false, "Vetem Drejtori i Inovacionit mund te fshije OKR te portofolit.");
            }

            var removed = _portfolioObjectives.RemoveAll(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                return (false, "Objektivi nuk u gjet.");
            }

            return await PersistSnapshotAsync()
                ? (true, null)
                : (false, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }
    private static bool TryValidatePortfolioObjectiveRequest(CreatePortfolioObjectiveRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.KeyResults.Count == 0)
        {
            error = "Objektivi dhe të paktën një KR janë të detyrueshme.";
            return false;
        }

        if (request.KeyResults.All(kr => string.IsNullOrWhiteSpace(kr.Title)))
        {
            error = "Të paktën një KR duhet të ketë titull.";
            return false;
        }

        error = null;
        return true;
    }

    private int GetNextPortfolioObjectiveNumber() =>
        _portfolioObjectives.Count == 0
            ? 1
            : _portfolioObjectives.Max(objective => ParsePrefixedNumber(objective.Id, "portfolio-")) + 1;

    public Task<IReadOnlyList<RiskDeviationResponse>> GetRiskDeviations(UserContext context) =>
        ExecuteReadAsync<IReadOnlyList<RiskDeviationResponse>>(() => GetVisibleProjects(context)
            .OrderByDescending(project => UrgencyRank(project))
            .ThenBy(project => project.DaysRemaining)
            .Select(project => new RiskDeviationResponse(
                project.Id,
                project.Code,
                project.Name,
                ProjectStatuses.ToLabel(project.Status),
                RiskLevels.ToLabel(project.Risk),
                project.Progress,
                project.ExpectedProgress,
                project.DeviationPercent,
                project.DaysRemaining,
                project.DelayDays,
                GetUrgencyLabel(project)))
            .ToList());

    public Task<IReadOnlyList<WeeklyUpdateResponse>> GetWeeklyUpdates(UserContext context, string? projectId) => ExecuteReadAsync<IReadOnlyList<WeeklyUpdateResponse>>(() =>
    {
        var visibleIds = GetVisibleProjects(context).Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _updates
            .Where(update => visibleIds.Contains(update.ProjectId) &&
                             (string.IsNullOrWhiteSpace(projectId) || string.Equals(update.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(update => update.SubmittedAt)
            .Select(update =>
            {
                var project = _projects.First(item => item.Id == update.ProjectId);
                return ToWeeklyUpdateResponse(update, project);
            })
            .ToList();
    });

    public async Task<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)> TryCreateWeeklyUpdateAsync(UserContext context, CreateWeeklyUpdateRequest request)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanSubmitUpdates(context.Role))
            {
                return (false, null, "Vetem ekspertet dhe drejtori mund te shtojne perditesime dyjavore.");
            }

            var project = GetVisibleProjects(context).FirstOrDefault(item => item.Id == request.ProjectId);
            if (project is null)
            {
                return (false, null, "Projekti nuk u gjet.");
            }

            var expertName = string.IsNullOrWhiteSpace(request.ExpertName)
                ? ApplicationRoles.ToDisplayLabel(context.Role)
                : request.ExpertName.Trim();

            var progress = Math.Clamp(request.Progress, 0, 100);
            var keyResultUpdateError = ApplyWeeklyKeyResultUpdates(project, request.KeyResults);
            if (keyResultUpdateError is not null)
            {
                return (false, null, keyResultUpdateError);
            }

            var update = new WeeklyUpdateState(
                $"upd-{GetNextWeeklyUpdateNumber()}",
                request.ProjectId,
                context.Username ?? ApplicationRoles.ToDisplayLabel(context.Role),
                context.Role,
                expertName,
                DateTimeOffset.UtcNow,
                progress,
                ResolveStatusForProgress(request.Status, progress),
                request.Risk,
                request.Blockers.Trim(),
                request.Comments.Trim(),
                ToWeeklyKeyResultStates(request.KeyResults));

            _updates.Add(update);
            project.Progress = progress;
            project.CurrentPhase = CalculateCurrentPhase(project.Progress, project.TotalPhases);
            project.Status = update.Status;
            project.Risk = request.Risk;
            project.LastUpdated = update.SubmittedAt;
            var updatesByProject = BuildUpdatesByProjectLookup();
            RecalculateProjectOkr(project, updatesByProject);

            var response = ToWeeklyUpdateResponse(update, project);

            return await PersistSnapshotAsync()
                ? (true, response, null)
                : (false, null, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    public async Task<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)> TryUpdateWeeklyUpdateAsync(UserContext context, string id, CreateWeeklyUpdateRequest request)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, WeeklyUpdateResponse? Response, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanSubmitUpdates(context.Role))
            {
                return (false, null, "Vetem ekspertet dhe drejtori mund te modifikojne perditesime.");
            }

            var visibleIds = GetVisibleProjects(context).Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var updateIndex = _updates.FindIndex(update => string.Equals(update.Id, id, StringComparison.OrdinalIgnoreCase) && visibleIds.Contains(update.ProjectId));
            if (updateIndex < 0)
            {
                return (false, null, "Perditesimi nuk u gjet.");
            }

            var currentUpdate = _updates[updateIndex];
            if (!CanMutateWeeklyUpdate(context, currentUpdate))
            {
                return (false, null, "Mund te modifikoni vetem perditesimet qe keni derguar ju.");
            }

            if (!string.Equals(currentUpdate.ProjectId, request.ProjectId, StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "Projekti i perditesimit nuk mund te ndryshohet.");
            }

            var project = _projects.FirstOrDefault(item => string.Equals(item.Id, currentUpdate.ProjectId, StringComparison.OrdinalIgnoreCase));
            if (project is null)
            {
                return (false, null, "Projekti nuk u gjet.");
            }

            var progress = Math.Clamp(request.Progress, 0, 100);
            var expertName = string.IsNullOrWhiteSpace(request.ExpertName)
                ? currentUpdate.ExpertName
                : request.ExpertName.Trim();
            var updated = currentUpdate with
            {
                ExpertName = expertName,
                Progress = progress,
                Status = ResolveStatusForProgress(request.Status, progress),
                Risk = request.Risk,
                Blockers = request.Blockers.Trim(),
                Comments = request.Comments.Trim(),
                KeyResults = request.KeyResults is null
                    ? currentUpdate.KeyResults
                    : ToWeeklyKeyResultStates(request.KeyResults)
            };

            var keyResultUpdateError = ApplyWeeklyKeyResultUpdates(project, request.KeyResults);
            if (keyResultUpdateError is not null)
            {
                return (false, null, keyResultUpdateError);
            }

            _updates[updateIndex] = updated;
            ApplyLatestUpdateState(project);
            RecalculateProjectOkr(project, BuildUpdatesByProjectLookup());

            var response = ToWeeklyUpdateResponse(updated, project);
            return await PersistSnapshotAsync()
                ? (true, response, null)
                : (false, null, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    public async Task<(bool IsSuccess, string? Error)> TryDeleteWeeklyUpdateAsync(UserContext context, string id)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanSubmitUpdates(context.Role))
            {
                return (false, "Vetem ekspertet dhe drejtori mund te fshijne perditesime.");
            }

            var visibleIds = GetVisibleProjects(context).Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var update = _updates.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase) && visibleIds.Contains(item.ProjectId));
            if (update is null)
            {
                return (false, "Perditesimi nuk u gjet.");
            }

            if (!CanMutateWeeklyUpdate(context, update))
            {
                return (false, "Mund te fshini vetem perditesimet qe keni derguar ju.");
            }

            var project = _projects.FirstOrDefault(item => string.Equals(item.Id, update.ProjectId, StringComparison.OrdinalIgnoreCase));
            _updates.Remove(update);
            if (project is not null)
            {
                ApplyLatestUpdateState(project);
                RecalculateProjectOkr(project, BuildUpdatesByProjectLookup());
            }

            return await PersistSnapshotAsync()
                ? (true, null)
                : (false, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    private static bool CanMutateWeeklyUpdate(UserContext context, WeeklyUpdateState update) =>
        context.Role != ApplicationRoles.StafAgjencie ||
        (!string.IsNullOrWhiteSpace(context.Username) &&
         string.Equals(update.SubmittedBy, context.Username, StringComparison.OrdinalIgnoreCase));

    private WeeklyUpdateResponse ToWeeklyUpdateResponse(WeeklyUpdateState update, ProjectState project) =>
        new(
            update.Id,
            update.ProjectId,
            project.Code,
            project.Name,
            update.SubmittedBy,
            ApplicationRoles.ToDisplayLabel(update.SubmittedRole),
            update.ExpertName,
            update.SubmittedAt,
            update.Progress,
            ProjectStatuses.ToLabel(update.Status),
            GetOkrAverage(project),
            RiskLevels.ToLabel(update.Risk),
            update.Blockers,
            update.Comments,
            update.KeyResults.Select(kr => new WeeklyUpdateKeyResultInput(kr.KeyResultId, kr.CurrentValue)).ToList());

    private void ApplyLatestUpdateState(ProjectState project)
    {
        var latestUpdate = _updates
            .Where(update => string.Equals(update.ProjectId, project.Id, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(update => update.SubmittedAt)
            .FirstOrDefault();

        if (latestUpdate is null)
        {
            return;
        }

        project.Progress = latestUpdate.Progress;
        project.CurrentPhase = CalculateCurrentPhase(project.Progress, project.TotalPhases);
        project.Status = latestUpdate.Status;
        project.Risk = latestUpdate.Risk;
        project.LastUpdated = latestUpdate.SubmittedAt;
    }

    private static string? ApplyWeeklyKeyResultUpdates(
        ProjectState project,
        IReadOnlyList<WeeklyUpdateKeyResultInput>? keyResultUpdates)
    {
        if (keyResultUpdates is null || keyResultUpdates.Count == 0)
        {
            return null;
        }

        var keyResultsById = project.Objectives
            .SelectMany(objective => objective.KeyResults)
            .ToDictionary(keyResult => keyResult.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var update in keyResultUpdates)
        {
            if (string.IsNullOrWhiteSpace(update.KeyResultId) ||
                !keyResultsById.TryGetValue(update.KeyResultId, out var keyResult))
            {
                return "Nje KR i derguar nuk i perket projektit te zgjedhur.";
            }

            keyResult.Progress = CalculateKeyResultProgress(update.CurrentValue, keyResult.Target);
        }

        return null;
    }

    private static List<WeeklyUpdateKeyResultState> ToWeeklyKeyResultStates(IReadOnlyList<WeeklyUpdateKeyResultInput>? keyResultUpdates) =>
        keyResultUpdates?
            .Where(update => !string.IsNullOrWhiteSpace(update.KeyResultId))
            .Select(update => new WeeklyUpdateKeyResultState(update.KeyResultId.Trim(), Math.Max(0, update.CurrentValue)))
            .ToList() ?? [];

    public Task<IReadOnlyList<ProjectChangeProposalResponse>> GetChangeProposals(UserContext context, string? projectId) => ExecuteReadAsync<IReadOnlyList<ProjectChangeProposalResponse>>(() =>
    {
        var visibleIds = GetVisibleProjects(context).Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _changeProposals
            .Where(proposal => visibleIds.Contains(proposal.ProjectId) &&
                               (string.IsNullOrWhiteSpace(projectId) || string.Equals(proposal.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(proposal => proposal.SubmittedAt)
            .Select(ToChangeProposalResponse)
            .ToList();
    });

    public async Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryCreateChangeProposalAsync(UserContext context, CreateProjectChangeProposalRequest request)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanProposeProjectChanges(context.Role))
            {
                return (false, null, "Vetem Ekspert Agjencie mund te propozoje ndryshime ne projekt.");
            }

            var project = GetVisibleProjects(context).FirstOrDefault(item => item.Id == request.ProjectId);
            if (project is null)
            {
                return (false, null, "Projekti nuk u gjet.");
            }

            var type = request.Type.Trim().ToLowerInvariant();
            if (type is not ("deadline" or "content"))
            {
                return (false, null, "Tipi i propozimit duhet te jete afat ose permbajtje.");
            }

            if (string.IsNullOrWhiteSpace(request.ProposedValue) || string.IsNullOrWhiteSpace(request.Reason))
            {
                return (false, null, "Ndryshimi i propozuar dhe arsyeja jane te detyrueshme.");
            }

            var proposal = new ProjectChangeProposalState(
                $"chg-{GetNextChangeProposalNumber()}",
                request.ProjectId,
                ApplicationRoles.ToDisplayLabel(context.Role),
                context.Role,
                DateTimeOffset.UtcNow,
                type,
                request.CurrentValue.Trim(),
                request.ProposedValue.Trim(),
                request.Reason.Trim(),
                ChangeProposalStatuses.Pending);

            _changeProposals.Add(proposal);
            var response = ToChangeProposalResponse(proposal);
            return await PersistSnapshotAsync()
                ? (true, response, null)
                : (false, null, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    public async Task<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)> TryResolveChangeProposalAsync(UserContext context, string id, string action)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, ProjectChangeProposalResponse? Response, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanManagePortfolio(context.Role))
            {
                return (false, null, "Vetem Drejtori i Agjencise dhe Drejtori i Inovacionit Publik mund te shqyrtojne propozime.");
            }

            var proposal = _changeProposals.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
            if (proposal is null)
            {
                return (false, null, "Propozimi nuk u gjet.");
            }

            var visibleProjectIds = GetVisibleProjects(context).Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!visibleProjectIds.Contains(proposal.ProjectId))
            {
                return (false, null, "Nuk keni akses te ky propozim.");
            }

            if (!string.Equals(proposal.Status, ChangeProposalStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "Ky propozim eshte zgjidhur tashme.");
            }

            var normalizedAction = action.Trim().ToLowerInvariant();
            if (normalizedAction is "approve" or "approved" or "mirato")
            {
                var project = _projects.First(item => item.Id == proposal.ProjectId);
                if (!TryApplyApprovedChangeProposal(project, proposal, out var error))
                {
                    return (false, null, error);
                }

                project.LastUpdated = DateTimeOffset.UtcNow;
                proposal.Status = ChangeProposalStatuses.Approved;
            }
            else if (normalizedAction is "reject" or "rejected" or "refuzo")
            {
                proposal.Status = ChangeProposalStatuses.Rejected;
            }
            else
            {
                return (false, null, "Veprimi duhet te jete approve ose reject.");
            }

            var response = ToChangeProposalResponse(proposal);
            return await PersistSnapshotAsync()
                ? (true, response, null)
                : (false, null, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    public async Task<(bool IsSuccess, string? Error)> TryDeleteChangeProposalAsync(UserContext context, string id)
    {
        return await ExecuteMutationAsync<(bool IsSuccess, string? Error)>(async () =>
        {
            if (!ApplicationRoles.CanDeleteChangeProposals(context.Role))
            {
                return (false, "Ky rol nuk mund te fshije propozime.");
            }

            var proposal = _changeProposals.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
            if (proposal is null)
            {
                return (false, "Propozimi nuk u gjet.");
            }

            var visibleProjectIds = GetVisibleProjects(context).Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!visibleProjectIds.Contains(proposal.ProjectId))
            {
                return (false, "Nuk keni akses te ky propozim.");
            }

            _changeProposals.Remove(proposal);
            return await PersistSnapshotAsync()
                ? (true, null)
                : (false, "Ndryshimi nuk u ruajt ne PostgreSQL. Provo perseri.");
        });
    }

    public Task<CalendarMonthResponse> GetCalendarMonth(UserContext context, DateOnly month) => ExecuteReadAsync(() =>
    {
        var events = GetVisibleProjects(context)
            .SelectMany(BuildProjectEvents)
            .OrderBy(item => item.Date)
            .ToList();

        var monthStart = new DateOnly(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var gridStart = StartOfWeek(monthStart);
        var gridEnd = EndOfWeek(monthEnd);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var days = new List<CalendarDayResponse>();
        for (var cursor = gridStart; cursor <= gridEnd; cursor = cursor.AddDays(1))
        {
            var dayEvents = events
                .Where(item => DateOnly.FromDateTime(item.Date.LocalDateTime) == cursor)
                .Select(item =>
                {
                    var project = _projects.First(project => project.Id == item.ProjectId);
                    return new CalendarDayEventResponse(
                        item.Id,
                        item.ProjectId,
                        project.Code,
                        project.Name,
                        item.Type,
                        EventTypes.ToLabel(item.Type),
                        item.Title);
                })
                .ToList();

            days.Add(new CalendarDayResponse(cursor, cursor.Month == monthStart.Month, cursor == today, dayEvents));
        }

        return new CalendarMonthResponse(monthStart, gridStart, gridEnd, days);
    });

    public Task<IReadOnlyList<UpcomingEventResponse>> GetUpcomingEvents(UserContext context, int limit) => ExecuteReadAsync<IReadOnlyList<UpcomingEventResponse>>(() =>
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return GetVisibleProjects(context)
            .SelectMany(BuildProjectEvents)
            .Where(item => DateOnly.FromDateTime(item.Date.LocalDateTime) >= today)
            .OrderBy(item => item.Date)
            .Take(limit)
            .Select(item =>
            {
                var project = _projects.First(projectState => projectState.Id == item.ProjectId);
                return new UpcomingEventResponse(
                    item.Id,
                    item.ProjectId,
                    item.Date,
                    item.Type,
                    EventTypes.ToLabel(item.Type),
                    item.Title,
                    project.Code,
                    project.Name);
            })
            .ToList();
    });

    public Task<IReadOnlyList<UpcomingEventResponse>> GetPastEvents(UserContext context, int limit) => ExecuteReadAsync<IReadOnlyList<UpcomingEventResponse>>(() =>
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return GetVisibleProjects(context)
            .SelectMany(BuildProjectEvents)
            .Where(item => DateOnly.FromDateTime(item.Date.LocalDateTime) < today)
            .OrderByDescending(item => item.Date)
            .Take(limit)
            .Select(item =>
            {
                var project = _projects.First(projectState => projectState.Id == item.ProjectId);
                return new UpcomingEventResponse(
                    item.Id,
                    item.ProjectId,
                    item.Date,
                    item.Type,
                    EventTypes.ToLabel(item.Type),
                    item.Title,
                    project.Code,
                    project.Name);
            })
            .ToList();
    });

    public async Task<AiChatResponse> GetAiChatReply(UserContext context, AiChatRequest request)
    {
        if (!AiChatLimits.TryValidate(request, out var validationError))
        {
            throw new ArgumentException(validationError, nameof(request));
        }

        var apiKey = _geminiOptions.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return await ExecuteReadAsync(() => BuildAiChatFallback(context, request));
        }

        try
        {
            var systemPrompt = await ExecuteReadAsync(() =>
            {
                var visible = GetVisibleProjects(context);
                var delayed = visible.Where(p => p.DelayDays > 0)
                                     .OrderByDescending(p => p.DelayDays)
                                     .Take(3).ToList();
                var avgOkr = visible.Count == 0 ? 0 : (int)Math.Round(visible.Average(GetOkrAverage));
                var highRisk = visible
                    .Where(p => p.Risk is RiskLevels.High or RiskLevels.Critical)
                    .Select(p => $"{p.Code} ({p.Name})")
                    .ToList();

                return $"""
            Jeni një asistent AI për platformën Innovation4Albania.
            Përgjigjuni GJITHMONË në shqip. Ji konciz dhe praktik.
            
            KONTEKSTI:
            - Roli: {ApplicationRoles.ToDisplayLabel(context.Role)}
            - Ministria: {context.Ministry ?? "Të gjitha"}
            - Projekte totale: {visible.Count}
            - OKR mesatar: {avgOkr}%
            - Projekte me vonesë: {string.Join(", ", delayed.Select(p => p.Code))}
            - Risk i lartë/kritik: {string.Join(", ", highRisk)}
            """;
            });

            var conversation = BuildGeminiConversation(request);
            var geminiRequest = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = conversation.Select(message => new
                {
                    role = message.Role,
                    parts = new[] { new { text = message.Content } }
                }).ToArray(),
                generationConfig = new { maxOutputTokens = 2048, temperature = 0.5 }
            };

            using var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

            var json = JsonSerializer.Serialize(geminiRequest);
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var httpResponse = await http.PostAsync(url, content);
            var responseBody = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini error {httpResponse.StatusCode}: {responseBody}");
            }

            var doc = JsonDocument.Parse(responseBody);
            var answer = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "Nuk u mor përgjigje.";

            return new AiChatResponse(
                new ChatMessageResponse($"ai-{Guid.NewGuid():N}", "assistant", answer, DateTimeOffset.UtcNow),
                ["Kontrollo projektet me devijim mbi 10%", "Verifiko KR-të me progres nën 60%", "Planifiko përditësimet javore"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini chat request failed");
            return await ExecuteReadAsync(() => BuildAiChatFallback(context, request));
        }
    }

    private static IReadOnlyList<GeminiChatMessage> BuildGeminiConversation(AiChatRequest request)
    {
        var conversation = (request.History ?? [])
            .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .Select(message => new GeminiChatMessage(ToGeminiRole(message.Role), message.Content.Trim()))
            .Where(message => !string.IsNullOrWhiteSpace(message.Role))
            .TakeLast(20)
            .ToList();

        if (!string.IsNullOrWhiteSpace(request.Message))
        {
            conversation.Add(new GeminiChatMessage("user", request.Message.Trim()));
        }

        return conversation;
    }

    private static string ToGeminiRole(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        "assistant" or "model" => "model",
        "user" => "user",
        _ => string.Empty
    };

    private sealed record GeminiChatMessage(string Role, string Content);

    private AiChatResponse BuildAiChatFallback(UserContext context, AiChatRequest request)
    {
        var visible = GetVisibleProjects(context);
        var delayed = visible
            .Where(project => project.DelayDays > 0)
            .OrderByDescending(project => project.DelayDays)
            .Take(3)
            .ToList();
        var avgOkr = visible.Count == 0 ? 0 : (int)Math.Round(visible.Average(GetOkrAverage));
        var highRisk = visible
            .Where(project => project.Risk is RiskLevels.High or RiskLevels.Critical)
            .OrderByDescending(project => project.DeviationPercent)
            .Take(3)
            .ToList();

        var answer = new StringBuilder()
            .Append($"Nga {visible.Count} projekte të aksesueshme, OKR mesatar është {avgOkr}%. ");

        if (highRisk.Count > 0)
        {
            answer.Append("Prioriteti kryesor është ndjekja e projekteve me risk të lartë: ")
                .Append(string.Join(", ", highRisk.Select(project => project.Code)))
                .Append(". ");
        }

        if (delayed.Count > 0)
        {
            answer.Append("Projektet me vonesën më të madhe janë ")
                .Append(string.Join(", ", delayed.Select(project => $"{project.Code} ({project.DelayDays} ditë)")))
                .Append(". ");
        }

        answer.Append("Pa Gemini:ApiKey po kthej analizë lokale bazuar në të dhënat e dashboard-it.");

        return new AiChatResponse(
            new ChatMessageResponse($"ai-{Guid.NewGuid():N}", "assistant", answer.ToString(), DateTimeOffset.UtcNow),
            ["Kontrollo projektet me devijim mbi 10%", "Verifiko KR-të me progres nën 60%", "Planifiko përditësimet javore"]);
    }

    private IReadOnlyList<ProjectState> GetVisibleProjects(UserContext context)
    {
        if (context.Role != ApplicationRoles.StafMinistrie)
        {
            return _projects;
        }

        var ministry = ResolveMinistry(context.Ministry);
        if (ministry is null)
        {
            return [];
        }

        return _projects
            .Where(project => project.Ministries.Contains(ministry, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private string? ResolveMinistry(string? ministry)
    {
        if (string.IsNullOrWhiteSpace(ministry))
        {
            return null;
        }

        var normalized = NormalizeForMinistryMatch(ministry);
        return _ministries.FirstOrDefault(item => NormalizeForMinistryMatch(item) == normalized);
    }

    private string NormalizeProposalStatus(string status)
    {
        var result = ChangeProposalStatuses.Normalize(status);
        if (!ChangeProposalStatuses.IsKnown(result))
        {
            _logger.LogWarning("Unrecognised change proposal status '{Status}' in snapshot — storing as-is.", status);
        }

        return result;
    }

    private static string NormalizeForMinistryMatch(string value)
    {
        var fixedValue = value
            .Trim()
            .Replace("\u00C3\u00AB", "ë", StringComparison.Ordinal)
            .Replace("\u00C3\u2039", "Ë", StringComparison.Ordinal)
            .Replace("\u00C3\u00A7", "ç", StringComparison.Ordinal)
            .Replace("\u00C3\u2021", "Ç", StringComparison.Ordinal)
            .Replace("\u00EF\u00BF\u00BD", "ë", StringComparison.Ordinal)
            .Replace('\uFFFD', 'ë');

        var decomposed = fixedValue.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                }

                previousWasSpace = true;
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
            previousWasSpace = false;
        }

        return builder.ToString().Trim();
    }

    private static string GetPerformanceBucket(int score) => score switch
    {
        >= 85 => PerformanceBuckets.Excellent,
        >= 70 => PerformanceBuckets.Good,
        >= 55 => PerformanceBuckets.NeedsAttention,
        _ => PerformanceBuckets.NeedsAttention
    };

    private static string ShortMinistryName(string ministry) => ministry switch
    {
        "Ministria e Infrastrukturës dhe Energjisë" => "M. e Infrastrukturës dhe Energjisë",
        "Ministria e Punëve të Brendshme" => "M. e Punëve të Brendshme",
        "Ministria për Evropën dhe Punët e Jashtme" => "M. për Evropën dhe Punët e Jashtme",
        "Ministria e Financave" => "M. e Financave",
        "Ministria e Kulturës dhe Turizmit" => "M. e Kulturës dhe Turizmit",
        "Ministria e Mjedisit" => "M. e Mjedisit",
        "Ministria e Shëndetësisë dhe Mirëqenies Sociale" => "M. e Shëndetësisë dhe Mirëqenies Sociale",
        "Ministria e Ekonomisë dhe Inovacionit" => "M. e Ekonomisë dhe Inovacionit",
        "Ministria e Drejtësisë" => "M. e Drejtësisë",
        "Ministria e Mbrojtjes" => "M. e Mbrojtjes",
        "Ministria e Bujqësisë dhe Zhvillimit Rural" => "M. e Bujqësisë dhe Zhvillimit Rural",
        "Ministria e Shtetit për Pushtetin Vendor" => "M. e Shtetit për Pushtetin Vendor",
        "Ministria e Shtetit për Administratën Publike dhe Antikorrupsionin" => "M. e Administratës Publike dhe Antikorrupsionit",
        "Ministria për Marrëdhëniet me Parlamentin" => "M. për Marrëdhëniet me Parlamentin",
        _ => ministry
    };

    private Dictionary<string, IReadOnlyList<WeeklyUpdateState>> BuildUpdatesByProjectLookup() =>
        _updates
            .GroupBy(update => update.ProjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WeeklyUpdateState>)group.OrderBy(update => update.SubmittedAt).ToList(),
                StringComparer.OrdinalIgnoreCase);

    private int GetOkrAverage(ProjectState project) =>
        CalculateOkrAverage(project.Okr);

    private void RecalculateAllProjectOkrs()
    {
        var updatesByProject = BuildUpdatesByProjectLookup();
        foreach (var project in _projects)
        {
            RecalculateProjectOkr(project, updatesByProject);
        }
    }

    private static void RecalculateProjectOkr(
        ProjectState project,
        IReadOnlyDictionary<string, IReadOnlyList<WeeklyUpdateState>> updatesByProject)
    {
        project.Okr = CalculateOkr(project, updatesByProject);
    }

    private static int CalculateOkrAverage(ProjectOkr okr) =>
        ClampPercent((okr.Deadlines + okr.Quality + okr.Impact + okr.Dynamics) / 4d);

    private static IReadOnlyList<WeeklyUpdateState> GetProjectUpdates(
        ProjectState project,
        IReadOnlyDictionary<string, IReadOnlyList<WeeklyUpdateState>> updatesByProject) =>
        updatesByProject.TryGetValue(project.Id, out var updates) ? updates : [];

    private static ProjectOkr CalculateOkr(
        ProjectState project,
        IReadOnlyDictionary<string, IReadOnlyList<WeeklyUpdateState>> updatesByProject)
    {
        var projectUpdates = GetProjectUpdates(project, updatesByProject);

        var deadline = CalculateDeadlineOkr(project);
        var quality = CalculateQualityOkr(projectUpdates);
        var impact = CalculateImpactOkr(project);
        var dynamics = CalculateDynamicsOkr(project, projectUpdates);

        return new ProjectOkr(
            ClampPercent(deadline),
            ClampPercent(quality),
            ClampPercent(impact),
            ClampPercent(dynamics));
    }

    private static int CalculateDeadlineOkr(ProjectState project)
    {
        if (project.Status == ProjectStatuses.Completed && project.LastUpdated <= project.EndDate)
            return 100;

        if (DateTimeOffset.UtcNow > project.EndDate)
            return 0;

        var expectedProgress = CalculateExpectedProgress(project.StartDate, project.EndDate);
        return ClampPercent(100 - (expectedProgress - project.Progress) * 2);
    }

    private static int CalculateQualityOkr(IReadOnlyList<WeeklyUpdateState> updates)
    {
        if (updates.Count == 0)
        {
            return 50;
        }

        var updatesWithoutBlockers = updates.Count(update =>
            string.IsNullOrWhiteSpace(update.Blockers) ||
            string.Equals(update.Blockers.Trim(), "Asnjë", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(update.Blockers.Trim(), "Asnje", StringComparison.OrdinalIgnoreCase));

        return ClampPercent(updatesWithoutBlockers * 100d / updates.Count);
    }

    private static int CalculateImpactOkr(ProjectState project)
    {
        var totalDays = Math.Max(1, (project.EndDate - project.StartDate).TotalDays);
        var elapsedDays = (DateTimeOffset.UtcNow - project.StartDate).TotalDays;

        if (elapsedDays <= 0)
        {
            return 50;
        }

        var expectedProgress = Math.Clamp((elapsedDays / totalDays) * 100d, 0, 100);
        if (expectedProgress <= 0)
        {
            return 50;
        }

        return ClampPercent(Math.Min(100, project.Progress / expectedProgress * 100d));
    }

    private static int CalculateDynamicsOkr(ProjectState project, IReadOnlyList<WeeklyUpdateState> updates)
    {
        if (updates.Count == 0)
        {
            return 50;
        }

        var updatesOnTime = 0;
        var previousDate = project.StartDate;
        foreach (var update in updates)
        {
            if ((update.SubmittedAt - previousDate).TotalDays <= project.UpdateCadenceDays)
            {
                updatesOnTime++;
            }

            previousDate = update.SubmittedAt;
        }

        var weight = Math.Min(1.0, updates.Count / 5.0);
        var score = updatesOnTime * 100d / updates.Count;
        return ClampPercent(50 + (score - 50) * weight);
    }

    private static ProjectOkr NeutralOkr() => new(50, 50, 50, 50);

    private static int ClampPercent(double value) => (int)Math.Round(Math.Clamp(value, 0, 100));

    private static int CalculateKeyResultProgress(int currentValue, int target)
    {
        if (target <= 0)
        {
            return currentValue > 0 ? 100 : 0;
        }

        return ClampPercent(currentValue / (double)target * 100d);
    }

    private static string ResolveStatusForProgress(string status, int progress) =>
        Math.Clamp(progress, 0, 100) >= 100 ? ProjectStatuses.Completed : status;

    private static int CalculateCurrentPhase(int progress, int totalPhases)
    {
        var phases = Math.Max(1, totalPhases);
        return Math.Clamp((int)Math.Ceiling(Math.Clamp(progress, 0, 100) / 100d * phases), 1, phases);
    }

    private static int CalculateExpectedProgress(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var totalDays = Math.Max(1, (endDate - startDate).TotalDays);
        var elapsedDays = Math.Clamp((DateTimeOffset.UtcNow - startDate).TotalDays, 0, totalDays);
        return (int)Math.Round((elapsedDays / totalDays) * 100);
    }

    private static int CalculateDelayDays(ProjectState project)
    {
        var deviation = project.ExpectedProgress - project.Progress;
        return deviation <= 0 ? 0 : (int)Math.Round(deviation * 0.8);
    }

    private static bool IsOverdue(ProjectState project)
    {
        if (project.Status is ProjectStatuses.Completed or ProjectStatuses.Cancelled)
        {
            return false;
        }

        return (DateTimeOffset.UtcNow - project.LastUpdated).TotalDays > project.UpdateCadenceDays;
    }

    private ProjectResponse ToResponse(ProjectState project)
    {
        var expectedProgress = CalculateExpectedProgress(project.StartDate, project.EndDate);
        var deviationPercent = expectedProgress - project.Progress;
        var daysRemaining = Math.Max(0, (int)Math.Ceiling((project.EndDate - DateTimeOffset.UtcNow).TotalDays));
        var delayDays = CalculateDelayDays(project);
        var okr = project.Okr;
        var okrAverage = CalculateOkrAverage(okr);
        var totalCapacityPercent = project.TeamMembers.Sum(member => member.AllocationPercent);

        return new ProjectResponse(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            project.Ministries.ToList(),
            project.Agency,
            project.Status,
            project.Priority,
            ProjectPriorities.ToLabel(project.Priority),
            project.Sector,
            ProjectSectors.ToLabel(project.Sector),
            project.TotalPhases,
            project.CurrentPhase,
            project.StartDate,
            project.EndDate,
            project.Progress,
            expectedProgress,
            deviationPercent,
            daysRemaining,
            delayDays,
            okr,
            project.Risk,
            project.Team.ToList(),
            project.TeamMembers.Select(ToTeamMemberResponse).ToList(),
            project.Lead,
            project.UpdateCadenceDays,
            project.LastUpdated,
            okrAverage,
            IsOverdue(project),
            totalCapacityPercent,
            project.Objectives.Select(ToObjectiveResponse).ToList());
    }

    private static ObjectiveResponse ToObjectiveResponse(ObjectiveState state)
    {
        var keyResults = state.KeyResults.Select(kr => new KeyResultResponse(kr.Id, kr.Title, kr.Progress, kr.Target, kr.Unit)).ToList();
        var progress = keyResults.Count == 0 ? 0 : (int)Math.Round(keyResults.Average(item => item.Progress));
        return new ObjectiveResponse(state.Id, state.Title, state.Owner, progress, keyResults);
    }

    private static ObjectiveState ToObjectiveState(string id, ObjectiveInput input) =>
        new(
            id,
            input.Title.Trim(),
            string.IsNullOrWhiteSpace(input.Owner) ? "Drejtoria e Inovacionit" : input.Owner.Trim(),
            input.KeyResults.Select((kr, index) => new KeyResultState($"{id}-kr-{index + 1}", kr.Title.Trim(), kr.Progress, kr.Target, kr.Unit.Trim())).ToList());

    private static ObjectiveState ToObjectiveState(ObjectiveResponse response) =>
        new(
            response.Id,
            response.Title,
            response.Owner,
            response.KeyResults.Select(kr => new KeyResultState(kr.Id, kr.Title, kr.Progress, kr.Target, kr.Unit)).ToList());

    private static ProjectSnapshot ToProjectSnapshot(ProjectState project) =>
        new(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            project.Ministries.ToList(),
            project.Agency,
            project.Status,
            project.Priority,
            project.Sector,
            project.TotalPhases,
            project.CurrentPhase,
            project.StartDate,
            project.EndDate,
            project.Progress,
            project.Okr,
            project.Risk,
            project.Team.ToList(),
            project.TeamMembers.Select(ToTeamMemberResponse).ToList(),
            project.Lead,
            project.UpdateCadenceDays,
            project.LastUpdated,
            project.Objectives.Select(ToObjectiveResponse).ToList());

    private static ProjectState ToProjectState(ProjectSnapshot response)
    {
        var teamMembers = response.TeamMembers.Select(ToTeamMemberState).ToList();
        var team = response.Team.Count > 0
            ? response.Team.ToList()
            : teamMembers.Select(member => member.Name).ToList();

        return new ProjectState(
            response.Id,
            response.Code,
            response.Name,
            response.Description,
            response.Ministries.ToList(),
            response.Agency,
            response.Status,
            response.Priority,
            response.Sector,
            response.TotalPhases,
            response.CurrentPhase,
            response.StartDate,
            response.EndDate,
            response.Progress,
            response.Okr ?? NeutralOkr(),
            response.Risk,
            team,
            teamMembers,
            response.Lead,
            response.UpdateCadenceDays,
            response.LastUpdated,
            response.Objectives.Select(ToObjectiveState).ToList());
    }

    private static WorkgroupMemberState ToTeamMemberState(WorkgroupMemberResponse response) =>
        new(
            response.Id,
            response.Name,
            response.Role,
            response.Unit,
            response.AllocationPercent);

    private static List<WorkgroupMemberState> BuildTeamMembersForRequest(int projectNumber, CreateProjectRequest request)
    {
        var structuredMembers = request.TeamMembers
            .Where(member => !string.IsNullOrWhiteSpace(member.Name))
            .Select((member, index) => new WorkgroupMemberState(
                $"team-{projectNumber}-{index + 1}",
                member.Name.Trim(),
                WorkgroupRoles.All.Contains(member.Role) ? member.Role : WorkgroupRoles.ProjectOfficer,
                string.IsNullOrWhiteSpace(member.Unit) ? "Njësi qendrore" : member.Unit.Trim(),
                Math.Clamp(member.AllocationPercent, 10, 100)))
            .ToList();

        if (structuredMembers.Count > 0)
        {
            return structuredMembers;
        }

        return request.Team
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .Select((name, index) => new WorkgroupMemberState(
                $"team-{projectNumber}-{index + 1}",
                name,
                index == 0 ? WorkgroupRoles.ProjectLead : WorkgroupRoles.ProjectOfficer,
                "Njësi qendrore",
                index == 0 ? 80 : 50))
            .ToList();
    }

    private static WorkgroupMemberResponse ToTeamMemberResponse(WorkgroupMemberState member) =>
        new(
            member.Id,
            member.Name,
            member.Role,
            WorkgroupRoles.ToLabel(member.Role),
            member.Unit,
            member.AllocationPercent);

    private static bool TryApplyApprovedChangeProposal(ProjectState project, ProjectChangeProposalState proposal, out string? error)
    {
        if (proposal.Type == "deadline")
        {
            if (!DateOnly.TryParse(proposal.ProposedValue, out var proposedDate) &&
                !DateOnly.TryParseExact(proposal.ProposedValue, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out proposedDate))
            {
                error = "Data e propozuar nuk është e vlefshme.";
                return false;
            }

            var nextEndDate = new DateTimeOffset(proposedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            if (nextEndDate < project.StartDate)
            {
                error = "Afati i ri nuk mund të jetë përpara datës së nisjes.";
                return false;
            }

            project.EndDate = nextEndDate;
            error = null;
            return true;
        }

        if (proposal.Type == "content")
        {
            if (string.IsNullOrWhiteSpace(proposal.ProposedValue))
            {
                error = "Përmbajtja e propozuar nuk mund të jetë bosh.";
                return false;
            }

            project.Description = proposal.ProposedValue.Trim();
            error = null;
            return true;
        }

        error = "Tipi i propozimit nuk mbështetet.";
        return false;
    }

    private ProjectChangeProposalResponse ToChangeProposalResponse(ProjectChangeProposalState proposal)
    {
        var project = _projects.First(item => item.Id == proposal.ProjectId);
        return new ProjectChangeProposalResponse(
            proposal.Id,
            proposal.ProjectId,
            project.Code,
            project.Name,
            proposal.SubmittedBy,
            ApplicationRoles.ToDisplayLabel(proposal.SubmittedRole),
            proposal.SubmittedAt,
            proposal.Type,
            proposal.Type == "deadline" ? "Ndryshim afati" : "Ndryshim përmbajtjeje",
            proposal.CurrentValue,
            proposal.ProposedValue,
            proposal.Reason,
            proposal.Status);
    }


    private sealed record AiInsightState(
        ProjectState Project,
        ProjectResponse Response);

    private static KeyValuePair<string, int> GetWeakestOkrDimension(ProjectOkr okr) =>
        GetOkrDimensions(okr).OrderBy(item => item.Value).First();

    private static KeyValuePair<string, int> GetStrongestOkrDimension(ProjectOkr okr) =>
        GetOkrDimensions(okr).OrderByDescending(item => item.Value).First();

    private static Dictionary<string, int> GetOkrDimensions(ProjectOkr okr) =>
        new()
        {
            ["afatet"] = okr.Deadlines,
            ["cilÃ«sia"] = okr.Quality,
            ["impakti"] = okr.Impact,
            ["dinamika"] = okr.Dynamics,
        };

    private async Task<AiInsightResponse> BuildAiInsights(
        ProjectState project,
        ProjectResponse snapshotResponse)
    {
        var response = snapshotResponse;
        var weakest = new Dictionary<string, int>
        {
            ["afatet"] = response.Okr.Deadlines,
            ["cilësia"] = response.Okr.Quality,
            ["impakti"] = response.Okr.Impact,
            ["dinamika"] = response.Okr.Dynamics,
        }.OrderBy(item => item.Value).First();

        var strongest = new Dictionary<string, int>
        {
            ["afatet"] = response.Okr.Deadlines,
            ["cilësia"] = response.Okr.Quality,
            ["impakti"] = response.Okr.Impact,
            ["dinamika"] = response.Okr.Dynamics,
        }.OrderByDescending(item => item.Value).First();

        var jsonStructure = "{\"summary\":\"max 1 fjali\",\"riskExplanation\":\"max 1 fjali\",\"riskScore\":0-100,\"riskPrediction\":\"max 1 fjali\",\"positives\":[\"1 fjali\",\"1 fjali\"],\"concerns\":[\"1 fjali\"],\"recommendations\":[\"1 fjali\",\"1 fjali\"]}";

        var apiKey = _geminiOptions.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BuildAiInsightsFallback(project, response, weakest, strongest);
        }

        var prompt = $"Je një analist i platformës Innovation4Albania.\n" +
             $"Analizo projektin dhe kthe VETËM një JSON objekt (pa markdown, pa backticks) me këtë strukturë:\n" +
             $"{jsonStructure}\n\n" +
             $"TË DHËNAT E PROJEKTIT:\n" +
             $"- Emri: {project.Name}\n" +
             $"- Statusi: {ProjectStatuses.ToLabel(project.Status)}\n" +
             $"- Progresi: {project.Progress}%\n" +
             $"- Progresi i pritur: {response.ExpectedProgress}%\n" +
             $"- Devijimi: {response.DeviationPercent}%\n" +
             $"- Risku: {RiskLevels.ToLabel(project.Risk)}\n" +
             $"- OKR mesatar: {response.OkrAverage}%\n" +
             $"- Afatet OKR: {response.Okr.Deadlines}%\n" +
             $"- Cilësia OKR: {response.Okr.Quality}%\n" +
             $"- Impakti OKR: {response.Okr.Impact}%\n" +
             $"- Dinamika OKR: {response.Okr.Dynamics}%\n" +
             $"- Ditë të mbetura: {response.DaysRemaining}\n" +
             $"- Vonesa (ditë): {response.DelayDays}\n" +
             $"- Fusha më e dobët: {weakest.Key} ({weakest.Value}%)\n" +
             $"- Fusha më e fortë: {strongest.Key} ({strongest.Value}%)\n\n" +
             $"RiskScore duhet të jetë 0 për risk minimal dhe 100 për risk ekstrem, duke peshuar riskun, devijimin, vonesat, OKR dhe afatin.\n" +
             $"Përgjigju VETËM me JSON. Gjithçka në shqip." +
             $"IMPORTANT: Mbaj çdo fushë MAKSIMUM 1 fjali. JSON duhet të jetë kompakt.";

        try
        {
            var geminiRequest = new
            {
                contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                generationConfig = new { maxOutputTokens = 2048, temperature = 0.4 }
            };

            using var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
            var json = JsonSerializer.Serialize(geminiRequest);
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var httpResponse = await http.PostAsync(url, content);
            var responseBody = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.IsSuccessStatusCode)
            {
                var doc = JsonDocument.Parse(responseBody);
                var rawText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "{}";

                _logger.LogDebug("Gemini raw insights response: {RawText}", rawText);

                // Pastro markdown nëse Gemini kthen ```json ... ```
                var cleanJson = rawText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

                _logger.LogDebug("Gemini cleaned insights JSON: {CleanJson}", cleanJson);
                var parsed = JsonDocument.Parse(cleanJson).RootElement;

                var summary = parsed.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";
                var riskExp = parsed.TryGetProperty("riskExplanation", out var r) ? r.GetString() ?? "" : "";
                var riskScore = parsed.TryGetProperty("riskScore", out var rs) && rs.TryGetInt32(out var parsedRiskScore)
                    ? Math.Clamp(parsedRiskScore, 0, 100)
                    : CalculateRiskScore(project, response);
                var riskPrediction = parsed.TryGetProperty("riskPrediction", out var rp) ? rp.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(riskPrediction))
                {
                    riskPrediction = BuildRiskPrediction(riskScore);
                }
                var positives = parsed.TryGetProperty("positives", out var pos)
                    ? pos.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                    : new List<string>();
                var concerns = parsed.TryGetProperty("concerns", out var con)
                    ? con.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                    : new List<string>();
                var recommendations = parsed.TryGetProperty("recommendations", out var rec)
                    ? rec.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                    : new List<string>();

                var attentionLevel = project.Risk switch
                {
                    RiskLevels.Critical => "critical",
                    RiskLevels.High => "high",
                    _ when response.DeviationPercent > 10 || response.IsOverdue => "medium",
                    _ => "normal"
                };

                var confidence = Math.Clamp(
                    65 + (response.DeviationPercent > 8 ? 10 : 0) + (project.Risk is RiskLevels.High or RiskLevels.Critical ? 10 : 0),
                    60, 95);

                return new AiInsightResponse(
                    project.Id, attentionLevel, summary, riskExp, riskScore, riskPrediction, confidence,
                    positives, concerns, recommendations);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini insights request failed for project {ProjectId}", project.Id);
        }

        // Fallback nese Gemini deshton
        return BuildAiInsightsFallback(project, response, weakest, strongest);
    }

    private static AiInsightResponse BuildAiInsightsFallback(
        ProjectState project, ProjectResponse response,
        KeyValuePair<string, int> weakest, KeyValuePair<string, int> strongest)
    {
        var concerns = new List<string>();
        var positives = new List<string>();
        var recommendations = new List<string>();
        var riskScore = CalculateRiskScore(project, response);

        if (response.DeviationPercent > 10)
        {
            concerns.Add($"Progresi aktual është {response.DeviationPercent}% poshtë ritmit të pritur.");
            recommendations.Add("Riplanifiko fazat që kanë mbetur.");
        }
        if (response.OkrAverage >= 80)
            positives.Add($"OKR mesatar është {response.OkrAverage}% — ekzekutim i qëndrueshëm.");

        positives.Add($"Indikatori më i fortë është {strongest.Key} me {strongest.Value}%.");
        concerns.Add($"Fusha që kërkon ndërhyrje: {weakest.Key} me {weakest.Value}%.");
        recommendations.Add($"Forco planin për {weakest.Key} në ciklin e ardhshëm.");

        var attentionLevel = project.Risk switch
        {
            RiskLevels.Critical => "critical",
            RiskLevels.High => "high",
            _ when response.DeviationPercent > 10 || response.IsOverdue => "medium",
            _ => "normal"
        };

        return new AiInsightResponse(
            project.Id, attentionLevel,
            $"Projekti në statusin {ProjectStatuses.ToLabel(project.Status)} me progres {project.Progress}% dhe OKR {response.OkrAverage}%.",
            $"Risku {RiskLevels.ToLabel(project.Risk).ToLowerInvariant()} lidhet me devijimin prej {response.DeviationPercent}%.",
            riskScore,
            BuildRiskPrediction(riskScore),
            Math.Clamp(65 + (response.DeviationPercent > 8 ? 10 : 0), 60, 95),
            positives, concerns.Distinct().ToList(), recommendations.Distinct().ToList());
    }

    private static int CalculateRiskScore(ProjectState project, ProjectResponse response)
    {
        var riskBase = project.Risk switch
        {
            RiskLevels.Critical => 75,
            RiskLevels.High => 55,
            RiskLevels.Medium => 30,
            _ => 8
        };
        var okrPenalty = Math.Max(0, 100 - response.OkrAverage) * 0.35;
        var progressPenalty = Math.Max(0, response.ExpectedProgress - project.Progress) * 0.45;
        var delayPenalty = Math.Min(20, Math.Max(0, response.DelayDays) * 0.8);
        var deadlinePenalty = response.DaysRemaining <= 30 && project.Progress < 90 ? 10 : 0;

        return Math.Clamp((int)Math.Round(riskBase + okrPenalty + progressPenalty + delayPenalty + deadlinePenalty), 0, 100);
    }

    private static string BuildRiskPrediction(int riskScore) =>
        riskScore switch
        {
            <= 20 => "Modeli sugjeron vazhdimësi normale me monitorim standard.",
            <= 45 => "Modeli sugjeron monitorim të rregullt dhe kontroll të ritmit të progresit.",
            <= 70 => "Modeli sugjeron vëmendje të shtuar dhe plan rikuperimi për faktorët më të dobët.",
            _ => "Modeli sugjeron ndërhyrje prioritare dhe eskalim drejtues për uljen e riskut."
        };

    private PortfolioMetricsResponse BuildPortfolioMetrics(IReadOnlyCollection<ProjectState> projects)
    {
        if (projects.Count == 0)
        {
            return new PortfolioMetricsResponse(0, 0, 0, 0);
        }

        var averageOkr = (int)Math.Round(projects.Average(GetOkrAverage));
        var onTime = (int)Math.Round(projects.Count(project => CalculateExpectedProgress(project.StartDate, project.EndDate) - project.Progress <= 5) * 100d / projects.Count);
        var deviationAverage = (int)Math.Round(projects.Average(project => Math.Max(0, CalculateExpectedProgress(project.StartDate, project.EndDate) - project.Progress)));
        var attention = projects.Count(project => project.Risk is RiskLevels.High or RiskLevels.Critical || CalculateDelayDays(project) > 7);
        return new PortfolioMetricsResponse(averageOkr, onTime, deviationAverage, attention);
    }

    private static string GetUrgencyLabel(ProjectState project)
    {
        var rank = UrgencyRank(project);
        return rank switch
        {
            >= 4 => "Urgjencë kritike",
            3 => "Urgjencë e lartë",
            2 => "Për monitorim",
            _ => "Stabile"
        };
    }

    private static int UrgencyRank(ProjectState project)
    {
        var score = 0;
        if (project.Risk == RiskLevels.Critical) score += 4;
        else if (project.Risk == RiskLevels.High) score += 3;
        else if (project.Risk == RiskLevels.Medium) score += 2;
        else score += 1;

        var delay = CalculateDelayDays(project);
        if (delay > 14) score += 2;
        else if (delay > 7) score += 1;

        if ((project.EndDate - DateTimeOffset.UtcNow).TotalDays < 30) score += 1;
        return score;
    }

    private static IReadOnlyList<ProjectEventResponse> BuildProjectEvents(ProjectState project) =>
    [
        new($"start-{project.Id}", project.Id, project.StartDate, EventTypes.Kickoff, "Nisja e projektit"),
        new($"end-{project.Id}", project.Id, project.EndDate, EventTypes.Completion, "Përfundimi i projektit")
    ];

    private static DateOnly StartOfWeek(DateOnly value)
    {
        var current = value;
        while (current.DayOfWeek != DayOfWeek.Monday)
        {
            current = current.AddDays(-1);
        }

        return current;
    }

    private static DateOnly EndOfWeek(DateOnly value)
    {
        var current = value;
        while (current.DayOfWeek != DayOfWeek.Sunday)
        {
            current = current.AddDays(1);
        }

        return current;
    }

    private static DateTimeOffset IsoOffset(int offsetDays) =>
        new(DateTime.UtcNow.Date.AddDays(offsetDays).AddHours(9), TimeSpan.Zero);

    private List<ProjectState> BuildProjects() =>
    [
        new(
            "p1",
            "ASHSH-2024",
            "ASHSH - Agjencia Shtetërore për Shpronësimin",
            "Projekt real demonstrues për transformimin e proceseve të shpronësimit dhe koordinimit ndërinstitucional.",
            ["Ministria e Infrastrukturës dhe Energjisë", "Ministria e Ekonomisë dhe Inovacionit"],
            "Agjencia Shtetërore për Shpronësimin",
            ProjectStatuses.Active,
            ProjectPriorities.Critical,
            ProjectSectors.PublicServices,
            10,
            7,
            IsoOffset(-220),
            IsoOffset(140),
            70,
            new ProjectOkr(80, 75, 70, 78),
            RiskLevels.Medium,
            ["Erblin Malkurti", "Evilsidio Tosku", "Nensi Ahmetbeja", "Ina Peleshka"],
            [
                new WorkgroupMemberState("team-1-1", "Erblin Malkurti", WorkgroupRoles.ProjectLead, "ASHSH", 90),
                new WorkgroupMemberState("team-1-2", "Evilsidio Tosku", WorkgroupRoles.BusinessAnalyst, "ASHSH", 70),
                new WorkgroupMemberState("team-1-3", "Nensi Ahmetbeja", WorkgroupRoles.MinistryRepresentative, "Ministria e Infrastrukturës dhe Energjisë", 60),
                new WorkgroupMemberState("team-1-4", "Ina Peleshka", WorkgroupRoles.OkrOwner, "Ministria e Ekonomisë dhe Inovacionit", 55)
            ],
            "Erblin Malkurti",
            14,
            IsoOffset(-5),
            [
                new ObjectiveState("obj-1", "Përshpejtimi i shpronësimeve", "ASHSH",
                [
                    new KeyResultState("obj-1-kr-1", "Ulja e kohës mesatare të shqyrtimit", 74, 100, "%"),
                    new KeyResultState("obj-1-kr-2", "Digjitalizimi i dosjeve prioritare", 68, 100, "%")
                ])
            ]),
        CreateActualProject(
            2,
            "CASHLESS-2026",
            "Cashless Albania",
            "Rritja e pagesave elektronike nga 16% në 60% dhe pranimi i metodave elektronike nga 100% e institucioneve publike.",
            ["Ministria e Financave", "Ministria e Ekonomisë dhe Inovacionit"],
            ProjectSectors.Governance,
            FixedDate(2026, 4, 28),
            FixedDate(2026, 12, 31),
            ["Eralda Alhysa", "Leah Hamiti", "Eneida Koci", "Artjola Ganellari", "Oraldo Hoxhallari", "Ardit Hysko", "Elird Kospiri"],
            "Albi Hoxha"),
        CreateActualProject(
            3,
            "ROAD-ZERO-2026",
            "Reduktimi drejt 0 i fataliteteve në rrugë",
            "Reduktimi i fataliteteve rrugore dhe aksidenteve nëpërmjet aplikimit të sistemeve inteligjente.",
            ["Ministria e Infrastrukturës dhe Energjisë"],
            ProjectSectors.Infrastructure,
            FixedDate(2026, 4, 23),
            FixedDate(2026, 12, 31),
            ["Evjenia Gjici", "Indrit Allaraj", "Evisildo Tosku", "Endrit Hoxha", "Dirseo Pasha"]),
        CreateActualProject(
            4,
            "CULTURE-REVENUE-2026",
            "Rritja e të ardhurave në objektet kulturore",
            "Rritja e të ardhurave në objektet kulturore.",
            ["Ministria e Kulturës dhe Turizmit"],
            ProjectSectors.PublicServices,
            FixedDate(2026, 4, 22),
            FixedDate(2026, 12, 31),
            ["Angjeliqi Karamano", "Blerta Zenelaj", "Kevin Ciko", "Ralf Cimbi", "Enton Spahiu", "Anxhela Alliaj"]),
        CreateActualProject(
            5,
            "SCHOOL-FOOD-2026",
            "Furnizimi me ushqim në shkolla",
            "Furnizimi me ushqim në shkolla.",
            ["Ministria e Kulturës dhe Turizmit"],
            ProjectSectors.PublicServices,
            FixedDate(2026, 4, 22),
            FixedDate(2026, 12, 31),
            ["Angjeliqi Karamano", "Blerta Zenelaj", "Kevin Ciko", "Ralf Cimbi", "Enton Spahiu", "Anxhela Alliaj"]),
        CreateActualProject(
            6,
            "SPORT-COMMUNITY-2026",
            "Kthimi i ambienteve sportive në qendra komunitare",
            "Kthimi i ambienteve sportive në qendra komunitare.",
            ["Ministria e Kulturës dhe Turizmit"],
            ProjectSectors.PublicServices,
            FixedDate(2026, 4, 22),
            FixedDate(2026, 12, 31),
            ["Angjeliqi Karamano", "Blerta Zenelaj", "Kevin Ciko", "Ralf Cimbi", "Enton Spahiu", "Anxhela Alliaj"]),
        CreateActualProject(
            7,
            "DIGITAL-TOOLS-2026",
            "Rritja e përdorimit të mjeteve digjitale me 80% në turizëm, bujqësi dhe mjedis",
            "Rritja e përdorimit të mjeteve digjitale me 80% në turizëm, bujqësi dhe mjedis.",
            ["Ministria e Kulturës dhe Turizmit", "Ministria e Bujqësisë dhe Zhvillimit Rural", "Ministria e Mjedisit"],
            ProjectSectors.PublicServices,
            FixedDate(2026, 4, 22),
            FixedDate(2026, 12, 31),
            ["Olsi Buna", "Nensi Ahmetbeja", "Basanja Shtylla", "Blerta Zenelaj", "Mustafa Llani"])
    ];

    private static ProjectState CreateActualProject(
        int idNumber,
        string code,
        string name,
        string description,
        IReadOnlyList<string> ministries,
        string sector,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        IReadOnlyList<string> innovationExperts,
        string? specialist = null)
    {
        var teamMembers = innovationExperts
            .Select((member, index) => new WorkgroupMemberState($"team-{idNumber}-{index + 1}", member, WorkgroupRoles.InnovationExpert, "Drejtoria e Inovacionit", 80))
            .ToList();

        if (!string.IsNullOrWhiteSpace(specialist))
        {
            teamMembers.Add(new WorkgroupMemberState($"team-{idNumber}-{teamMembers.Count + 1}", specialist, WorkgroupRoles.Specialist, "Drejtoria e Inovacionit", 70));
        }

        return new ProjectState(
            $"p{idNumber}",
            code,
            name,
            description,
            ministries.ToList(),
            null,
            ProjectStatuses.Active,
            ProjectPriorities.High,
            sector,
            6,
            1,
            startDate,
            endDate,
            10,
            new ProjectOkr(20, 25, 30, 35),
            RiskLevels.Medium,
            teamMembers.Select(member => member.Name).ToList(),
            teamMembers,
            teamMembers[0].Name,
            14,
            DateTimeOffset.UtcNow,
            BuildActualObjectives($"obj-{idNumber}", name, description));
    }

    private static DateTimeOffset FixedDate(int year, int month, int day) =>
        new(new DateTime(year, month, day, 9, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);

    private static List<ObjectiveState> BuildActualObjectives(string idPrefix, string title, string description) =>
    [
        new($"{idPrefix}-1", title,
        "Drejtoria e Inovacionit",
        [
            new KeyResultState($"{idPrefix}-kr-1", description, 10, 100, "%")
        ])
    ];

    private static ProjectState CreateSampleProject(int idNumber, int projectNumber, string ministry, string status, int progress, string risk, DateTimeOffset lastUpdated)
    {
        var totalPhases = status == ProjectStatuses.Completed ? 6 : 8;
        var currentPhase = status == ProjectStatuses.Completed ? totalPhases : Math.Clamp((int)Math.Ceiling(progress / 100d * totalPhases), 1, totalPhases);
        var deadlineScore = Math.Clamp(progress + 18, 35, 95);
        var qualityScore = Math.Clamp(progress + 28, 45, 96);
        var impactScore = Math.Clamp(progress + 24, 45, 97);
        var dynamicsScore = Math.Clamp(progress + 16, 38, 95);
        var priority = (projectNumber % 4) switch
        {
            0 => ProjectPriorities.Critical,
            1 => ProjectPriorities.High,
            2 => ProjectPriorities.Medium,
            _ => ProjectPriorities.Low
        };
        var sector = (projectNumber % 6) switch
        {
            0 => ProjectSectors.Infrastructure,
            1 => ProjectSectors.Digitalization,
            2 => ProjectSectors.Governance,
            3 => ProjectSectors.PublicServices,
            4 => ProjectSectors.Education,
            _ => ProjectSectors.Environment
        };
        var members = new List<WorkgroupMemberState>
        {
            new($"team-{idNumber}-1", $"Përgjegjësi {projectNumber}", WorkgroupRoles.ProjectLead, ministry, 80),
            new($"team-{idNumber}-2", $"Koordinator {projectNumber}", WorkgroupRoles.TechnicalCoordinator, "Njësi teknike", 60),
            new($"team-{idNumber}-3", $"Analist {projectNumber}", WorkgroupRoles.BusinessAnalyst, "Njësi projekti", 45)
        };

        return new ProjectState(
            $"p{idNumber}",
            $"PRJ-{projectNumber:000}",
            $"Projekti {projectNumber}",
            $"Projekt shembull për demonstrim të platformës për {ministry}.",
            [ministry],
            null,
            status,
            priority,
            sector,
            totalPhases,
            currentPhase,
            IsoOffset(-90 - (projectNumber * 12)),
            IsoOffset(status == ProjectStatuses.Completed ? -15 : 120 + (projectNumber * 20)),
            progress,
            new ProjectOkr(deadlineScore, qualityScore, impactScore, dynamicsScore),
            risk,
            members.Select(member => member.Name).ToList(),
            members,
            $"Përgjegjësi {projectNumber}",
            14,
            lastUpdated,
            BuildSampleObjectives($"obj-{idNumber}", $"Objektivi {projectNumber}"));
    }

    private static List<ObjectiveState> BuildPortfolioObjectives() =>
    [
        new("portfolio-1", "Rritja e dorëzimeve në kohë", "Drejtoria e Inovacionit",
        [
            new KeyResultState("portfolio-1-kr-1", "Të arrihet 82% dorëzim në kohë", 76, 82, "%"),
            new KeyResultState("portfolio-1-kr-2", "Të ulen devijimet mesatare nën 8%", 61, 8, "%")
        ]),
        new("portfolio-2", "Rritja e maturitetit OKR", "Drejtoria e Inovacionit",
        [
            new KeyResultState("portfolio-2-kr-1", "Mesatarja e OKR të portofolit", 73, 80, "%"),
            new KeyResultState("portfolio-2-kr-2", "Projektet me KR të përditësuar çdo 14 ditë", 68, 90, "%")
        ])
    ];

    private List<WeeklyUpdateState> BuildUpdates() =>
    [
        new("upd-1", "p1", "Drejtori i Inovacionit", ApplicationRoles.DrejtorAgjencie, "Drejtori i Inovacionit", IsoOffset(-2), 70, ProjectStatuses.Active, RiskLevels.Medium, "Koordinimi me dy ministritë kërkon sinkronizim më të shpeshtë.", "Faza 7 po ecën sipas planit, por duhen finalizuar vendimet e ndërmjetme.", []),
        new("upd-2", "p3", "Ekspert Agjencie", ApplicationRoles.StafAgjencie, "Ekspert Agjencie", IsoOffset(-6), 33, ProjectStatuses.Active, RiskLevels.High, "Ka vonesë në miratimin e dokumenteve përgatitore.", "Duhet ndjekje e përditshme me njësinë përkatëse.", []),
        new("upd-3", "p5", "Ekspert Agjencie", ApplicationRoles.StafAgjencie, "Ekspert Agjencie", IsoOffset(-8), 41, ProjectStatuses.Blocked, RiskLevels.High, "Bllokim në furnizim dhe mungesë aprovimesh.", "Kërkohet vendim drejtues për të zhbllokuar varësitë.", [])
    ];

    private static List<ObjectiveState> BuildSampleObjectives(string prefix, string title) =>
    [
        new(prefix, title, "Drejtoria e Inovacionit",
        [
            new KeyResultState($"{prefix}-kr-1", "KR 1", 62, 100, "%"),
            new KeyResultState($"{prefix}-kr-2", "KR 2", 48, 100, "%")
        ])
    ];

    private sealed record DashboardStoreSnapshot(
        IReadOnlyList<ProjectSnapshot> Projects,
        IReadOnlyList<ObjectiveResponse> PortfolioObjectives,
        IReadOnlyList<WeeklyUpdateSnapshot> Updates,
        IReadOnlyList<ProjectChangeProposalSnapshot> ChangeProposals);

    private sealed record ProjectSnapshot(
        string Id,
        string Code,
        string Name,
        string Description,
        IReadOnlyList<string> Ministries,
        string? Agency,
        string Status,
        string Priority,
        string Sector,
        int TotalPhases,
        int CurrentPhase,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        int Progress,
        ProjectOkr? Okr,
        string Risk,
        IReadOnlyList<string> Team,
        IReadOnlyList<WorkgroupMemberResponse> TeamMembers,
        string Lead,
        int UpdateCadenceDays,
        DateTimeOffset LastUpdated,
        IReadOnlyList<ObjectiveResponse> Objectives);

    private sealed record WeeklyUpdateSnapshot(
        string Id,
        string ProjectId,
        string SubmittedBy,
        string SubmittedRole,
        DateTimeOffset SubmittedAt,
        int Progress,
        string Status,
        string Risk,
        string Blockers,
        string Comments,
        string? ExpertName = null,
        IReadOnlyList<WeeklyUpdateKeyResultInput>? KeyResults = null);

    private sealed record ProjectChangeProposalSnapshot(
        string Id,
        string ProjectId,
        string SubmittedBy,
        string SubmittedRole,
        DateTimeOffset SubmittedAt,
        string Type,
        string CurrentValue,
        string ProposedValue,
        string Reason,
        string Status);

    private sealed class ProjectState(
        string id,
        string code,
        string name,
        string description,
        List<string> ministries,
        string? agency,
        string status,
        string priority,
        string sector,
        int totalPhases,
        int currentPhase,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int progress,
        ProjectOkr okr,
        string risk,
        List<string> team,
        List<WorkgroupMemberState> teamMembers,
        string lead,
        int updateCadenceDays,
        DateTimeOffset lastUpdated,
        List<ObjectiveState> objectives)
    {
        public string Id { get; } = id;
        public string Code { get; set; } = code;
        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public List<string> Ministries { get; } = ministries;
        public string? Agency { get; set; } = agency;
        public string Status { get; set; } = status;
        public string Priority { get; set; } = priority;
        public string Sector { get; set; } = sector;
        public int TotalPhases { get; set; } = totalPhases;
        public int CurrentPhase { get; set; } = currentPhase;
        public DateTimeOffset StartDate { get; set; } = startDate;
        public DateTimeOffset EndDate { get; set; } = endDate;
        public int Progress { get; set; } = progress;
        public ProjectOkr Okr { get; set; } = okr;
        public string Risk { get; set; } = risk;
        public List<string> Team { get; } = team;
        public List<WorkgroupMemberState> TeamMembers { get; } = teamMembers;
        public string Lead { get; set; } = lead;
        public int UpdateCadenceDays { get; set; } = updateCadenceDays;
        public DateTimeOffset LastUpdated { get; set; } = lastUpdated;
        public List<ObjectiveState> Objectives { get; } = objectives;
        public int TotalCapacityPercent => TeamMembers.Sum(member => member.AllocationPercent);
        public int OkrAverage => CalculateOkrAverage(Okr);
        public int ExpectedProgress => CalculateExpectedProgress(StartDate, EndDate);
        public int DelayDays => CalculateDelayDays(this);
        public int DeviationPercent => ExpectedProgress - Progress;
        public int DaysRemaining => Math.Max(0, (int)Math.Ceiling((EndDate - DateTimeOffset.UtcNow).TotalDays));
    }

    private sealed record ObjectiveState(string Id, string Title, string Owner, List<KeyResultState> KeyResults);

    private sealed record KeyResultState(string Id, string Title, int InitialProgress, int Target, string Unit)
    {
        public int Progress { get; set; } = InitialProgress;
    }

    private sealed record WorkgroupMemberState(
        string Id,
        string Name,
        string Role,
        string Unit,
        int AllocationPercent);

    private sealed record WeeklyUpdateState(
        string Id,
        string ProjectId,
        string SubmittedBy,
        string SubmittedRole,
        string ExpertName,
        DateTimeOffset SubmittedAt,
        int Progress,
        string Status,
        string Risk,
        string Blockers,
        string Comments,
        List<WeeklyUpdateKeyResultState> KeyResults);

    private sealed record WeeklyUpdateKeyResultState(string KeyResultId, int CurrentValue);

    private sealed class ProjectChangeProposalState(
        string Id,
        string ProjectId,
        string SubmittedBy,
        string SubmittedRole,
        DateTimeOffset SubmittedAt,
        string Type,
        string CurrentValue,
        string ProposedValue,
        string Reason,
        string Status)
    {
        public string Id { get; } = Id;
        public string ProjectId { get; } = ProjectId;
        public string SubmittedBy { get; } = SubmittedBy;
        public string SubmittedRole { get; } = SubmittedRole;
        public DateTimeOffset SubmittedAt { get; } = SubmittedAt;
        public string Type { get; } = Type;
        public string CurrentValue { get; } = CurrentValue;
        public string ProposedValue { get; } = ProposedValue;
        public string Reason { get; } = Reason;
        public string Status { get; set; } = Status;
    }
}

internal static class ObjectPipeExtensions
{
    public static TResult Pipe<TSource, TResult>(this TSource source, Func<TSource, TResult> selector) => selector(source);
}








