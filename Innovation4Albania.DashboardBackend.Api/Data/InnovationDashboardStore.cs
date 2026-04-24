using System.Globalization;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Data;

public sealed class InnovationDashboardStore
{
    private static readonly CultureInfo AlbanianCulture = CultureInfo.GetCultureInfo("sq-AL");

    private readonly IReadOnlyList<string> _ministries =
    [
        "Ministria e Infrastrukturës dhe Energjisë",
        "Ministria e Ekonomisë, Kulturës dhe Inovacionit",
        "Ministria e Financave",
        "Ministria e Arsimit, Sportit dhe Rinisë",
        "Ministria e Shëndetësisë dhe Mbrojtjes Sociale",
        "Ministria e Bujqësisë dhe Zhvillimit Rural",
        "Ministria e Drejtësisë",
        "Ministria e Brendshme",
        "Ministria për Evropën dhe Punët e Jashtme",
        "Ministria e Mbrojtjes",
        "Ministria e Turizmit dhe Mjedisit",
        "Ministria e Shtetit për Pushtetin Vendor"
    ];

    private readonly List<ProjectState> _projects;
    private readonly List<ObjectiveState> _portfolioObjectives;
    private readonly List<WeeklyUpdateState> _updates;
    private readonly List<ProjectChangeProposalState> _changeProposals;

    public InnovationDashboardStore()
    {
        _projects = BuildProjects();
        _portfolioObjectives = BuildPortfolioObjectives();
        _updates = BuildUpdates();
        _changeProposals = [];
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

            if (!_ministries.Contains(context.Ministry))
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
        var displayName = string.IsNullOrWhiteSpace(request.Name)
            ? ApplicationRoles.ToDisplayLabel(context.Role)
            : request.Name.Trim();

        return new UserResponse(
            $"{context.Role}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            displayName,
            context.Role,
            context.Ministry,
            ApplicationRoles.ToDisplayLabel(context.Role));
    }

    public DashboardSummaryResponse GetDashboardSummary(UserContext context)
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
    }

    public IReadOnlyList<StatusDistributionItem> GetStatusDistribution(UserContext context)
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
    }

    public IReadOnlyList<MinistryDistributionItem> GetMinistryDistribution(UserContext context)
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

        return _ministries
            .Select((ministry, index) => new MinistryDistributionItem(
                ShortMinistryName(ministry),
                visible.Count(project => project.Ministries.Contains(ministry)),
                palette[index % palette.Length]))
            .Where(item => item.Value > 0)
            .ToList();
    }

    public IReadOnlyList<PerformanceScoreItem> GetPerformanceScores(UserContext context) =>
        GetVisibleProjects(context)
            .OrderByDescending(project => project.OkrAverage)
            .Select(project => new PerformanceScoreItem(project.Id, project.Code, project.Name, project.OkrAverage, project.Progress, project.Risk))
            .ToList();

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

    public IReadOnlyList<ProjectResponse> GetProjects(UserContext context, string? status, string? query)
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
                    project.Ministries.Any(ministry => ministry.Contains(normalized, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return visible
            .OrderByDescending(project => project.DelayDays > 0)
            .ThenByDescending(project => project.DeviationPercent)
            .ThenBy(project => project.Name)
            .Select(ToResponse)
            .ToList();
    }

    public ProjectResponse? GetProjectById(string id, UserContext context) =>
        GetVisibleProjects(context)
            .FirstOrDefault(project => string.Equals(project.Id, id, StringComparison.OrdinalIgnoreCase))
            ?.Pipe(ToResponse);

    public bool TryCreateProject(UserContext context, CreateProjectRequest request, out ProjectResponse? response, out string? error)
    {
        response = null;

        if (!ApplicationRoles.CanCreateProjects(context.Role))
        {
            error = "Vetëm Drejtori i Inovacionit mund të krijojë projekte.";
            return false;
        }

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

        var now = DateTimeOffset.UtcNow;
        var project = new ProjectState(
            $"p{_projects.Count + 1}",
            request.Code.Trim(),
            request.Name.Trim(),
            request.Description.Trim(),
            request.Ministries.Count == 0 ? ["—"] : request.Ministries.Select(item => item.Trim()).ToList(),
            string.IsNullOrWhiteSpace(request.Agency) ? null : request.Agency.Trim(),
            request.Status,
            request.TotalPhases,
            request.CurrentPhase,
            request.StartDate,
            request.EndDate,
            request.Progress,
            request.Okr,
            request.Risk,
            request.Team.Select(item => item.Trim()).Where(item => item.Length > 0).ToList(),
            request.Lead.Trim(),
            request.UpdateCadenceDays,
            now,
            request.Objectives.Select((objective, index) => ToObjectiveState($"obj-{_projects.Count + 1}-{index + 1}", objective)).ToList());

        _projects.Add(project);
        response = ToResponse(project);
        error = null;
        return true;
    }

    public IReadOnlyList<ProjectEventResponse> GetEventsForProject(string projectId, UserContext context)
    {
        var project = GetVisibleProjects(context).FirstOrDefault(item => item.Id == projectId);
        return project is null ? [] : BuildProjectEvents(project);
    }

    public AiInsightResponse? GetProjectAiInsights(string projectId, UserContext context)
    {
        var project = GetVisibleProjects(context).FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            return null;
        }

        return BuildAiInsights(project);
    }

    public IReadOnlyList<PerformanceBoardColumnResponse> GetPerformanceBoard(UserContext context)
    {
        var scored = GetVisibleProjects(context)
            .Select(project => new PerformanceScoreItem(project.Id, project.Code, project.Name, project.OkrAverage, project.Progress, project.Risk))
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
                PerformanceBuckets.Critical,
                PerformanceBuckets.ToLabel(PerformanceBuckets.Critical),
                "< 55",
                scored.Where(item => GetPerformanceBucket(item.Score) == PerformanceBuckets.Critical).OrderByDescending(item => item.Score).ToList())
        ];
    }

    public PortfolioOkrResponse GetPortfolioOkr(UserContext context)
    {
        var visible = GetVisibleProjects(context);
        var objectives = _portfolioObjectives.Select(ToObjectiveResponse).ToList();
        return new PortfolioOkrResponse(BuildPortfolioMetrics(visible), objectives);
    }

    public bool TryCreatePortfolioObjective(UserContext context, CreatePortfolioObjectiveRequest request, out ObjectiveResponse? response, out string? error)
    {
        response = null;

        if (!ApplicationRoles.CanManagePortfolio(context.Role))
        {
            error = "Vetëm Drejtori i Inovacionit mund të shtojë OKR të portofolit.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.KeyResults.Count == 0)
        {
            error = "Objektivi dhe të paktën një KR janë të detyrueshme.";
            return false;
        }

        var state = ToObjectiveState($"portfolio-{_portfolioObjectives.Count + 1}", new ObjectiveInput(request.Title, request.Owner, request.KeyResults));
        _portfolioObjectives.Add(state);
        response = ToObjectiveResponse(state);
        error = null;
        return true;
    }

    public IReadOnlyList<RiskDeviationResponse> GetRiskDeviations(UserContext context) =>
        GetVisibleProjects(context)
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
            .ToList();

    public IReadOnlyList<WeeklyUpdateResponse> GetWeeklyUpdates(UserContext context, string? projectId)
    {
        var visibleIds = GetVisibleProjects(context).Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _updates
            .Where(update => visibleIds.Contains(update.ProjectId) &&
                             (string.IsNullOrWhiteSpace(projectId) || string.Equals(update.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(update => update.SubmittedAt)
            .Select(update =>
            {
                var project = _projects.First(item => item.Id == update.ProjectId);
                return new WeeklyUpdateResponse(
                    update.Id,
                    update.ProjectId,
                    project.Code,
                    project.Name,
                    update.SubmittedBy,
                    ApplicationRoles.ToDisplayLabel(update.SubmittedRole),
                    update.SubmittedAt,
                    update.Progress,
                    ProjectStatuses.ToLabel(update.Status),
                    CalculateOkrAverage(update.Okr),
                    RiskLevels.ToLabel(update.Risk),
                    update.Blockers,
                    update.Comments);
            })
            .ToList();
    }

    public bool TryCreateWeeklyUpdate(UserContext context, CreateWeeklyUpdateRequest request, out WeeklyUpdateResponse? response, out string? error)
    {
        response = null;

        if (!ApplicationRoles.CanSubmitUpdates(context.Role))
        {
            error = "Vetëm ekspertët dhe drejtori mund të shtojnë përditësime.";
            return false;
        }

        var project = GetVisibleProjects(context).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            error = "Projekti nuk u gjet.";
            return false;
        }

        var update = new WeeklyUpdateState(
            $"upd-{_updates.Count + 1}",
            request.ProjectId,
            ApplicationRoles.ToDisplayLabel(context.Role),
            context.Role,
            DateTimeOffset.UtcNow,
            request.Progress,
            request.Status,
            request.Okr,
            request.Risk,
            request.Blockers.Trim(),
            request.Comments.Trim());

        _updates.Add(update);

        project.Progress = request.Progress;
        project.Status = request.Status;
        project.Okr = request.Okr;
        project.Risk = request.Risk;
        project.LastUpdated = update.SubmittedAt;

        response = new WeeklyUpdateResponse(
            update.Id,
            update.ProjectId,
            project.Code,
            project.Name,
            update.SubmittedBy,
            ApplicationRoles.ToDisplayLabel(update.SubmittedRole),
            update.SubmittedAt,
            update.Progress,
            ProjectStatuses.ToLabel(update.Status),
            CalculateOkrAverage(update.Okr),
            RiskLevels.ToLabel(update.Risk),
            update.Blockers,
            update.Comments);
        error = null;
        return true;
    }

    public IReadOnlyList<ProjectChangeProposalResponse> GetChangeProposals(UserContext context, string? projectId)
    {
        var visibleIds = GetVisibleProjects(context).Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _changeProposals
            .Where(proposal => visibleIds.Contains(proposal.ProjectId) &&
                               (string.IsNullOrWhiteSpace(projectId) || string.Equals(proposal.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(proposal => proposal.SubmittedAt)
            .Select(ToChangeProposalResponse)
            .ToList();
    }

    public bool TryCreateChangeProposal(UserContext context, CreateProjectChangeProposalRequest request, out ProjectChangeProposalResponse? response, out string? error)
    {
        response = null;

        if (!ApplicationRoles.CanProposeProjectChanges(context.Role))
        {
            error = "Vetëm Ekspert Agjencie mund të propozojë ndryshime në projekt.";
            return false;
        }

        var project = GetVisibleProjects(context).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            error = "Projekti nuk u gjet.";
            return false;
        }

        var type = request.Type.Trim().ToLowerInvariant();
        if (type is not ("deadline" or "content"))
        {
            error = "Tipi i propozimit duhet të jetë afat ose përmbajtje.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ProposedValue) || string.IsNullOrWhiteSpace(request.Reason))
        {
            error = "Ndryshimi i propozuar dhe arsyeja janë të detyrueshme.";
            return false;
        }

        var proposal = new ProjectChangeProposalState(
            $"chg-{_changeProposals.Count + 1}",
            request.ProjectId,
            ApplicationRoles.ToDisplayLabel(context.Role),
            context.Role,
            DateTimeOffset.UtcNow,
            type,
            request.CurrentValue.Trim(),
            request.ProposedValue.Trim(),
            request.Reason.Trim(),
            "Në shqyrtim");

        _changeProposals.Add(proposal);
        response = ToChangeProposalResponse(proposal);
        error = null;
        return true;
    }

    public CalendarMonthResponse GetCalendarMonth(UserContext context, DateOnly month)
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
    }

    public IReadOnlyList<object> GetUpcomingEvents(UserContext context, int limit)
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
                return (object)new
                {
                    item.Id,
                    item.ProjectId,
                    item.Date,
                    item.Type,
                    typeLabel = EventTypes.ToLabel(item.Type),
                    item.Title,
                    projectCode = project.Code,
                    projectName = project.Name
                };
            })
            .ToList();
    }

    public AiChatResponse GetAiChatReply(UserContext context, AiChatRequest request)
    {
        var visible = GetVisibleProjects(context);
        var highestRisk = visible.OrderByDescending(UrgencyRank).FirstOrDefault();
        var delayed = visible.Where(project => project.DelayDays > 0).OrderByDescending(project => project.DelayDays).Take(3).ToList();
        var avgOkr = visible.Count == 0 ? 0 : (int)Math.Round(visible.Average(project => project.OkrAverage));

        var question = request.Message.Trim();
        var answer =
            $"Nga analiza e portofolit për {ApplicationRoles.ToDisplayLabel(context.Role)}, OKR mesatar është {avgOkr}% dhe projekti që kërkon më shumë vëmendje është " +
            $"{highestRisk?.Name ?? "nuk ka"}." +
            (delayed.Count > 0
                ? $" Projektet me devijimin më të madh janë: {string.Join(", ", delayed.Select(project => project.Code))}."
                : " Aktualisht nuk ka projekte me vonesa kritike.");

        if (question.Contains("okr", StringComparison.OrdinalIgnoreCase))
        {
            answer += " Fokusoni ndërhyrjen te KR-të me progres nën 60% dhe te projektet me devijim pozitiv mbi 12%.";
        }
        else if (question.Contains("risk", StringComparison.OrdinalIgnoreCase) || question.Contains("rrezik", StringComparison.OrdinalIgnoreCase))
        {
            answer += " Prioritet i parë duhet të jenë projektet me risk kritik dhe me afat nën 30 ditë.";
        }

        return new AiChatResponse(
            new ChatMessageResponse($"ai-{Guid.NewGuid():N}", "assistant", answer, DateTimeOffset.UtcNow),
            [
                "Kontrollo projektet me devijim mbi 10%",
                "Verifiko KR-të me progres nën 60%",
                "Planifiko përditësimet javore të vonuara"
            ]);
    }

    private IReadOnlyList<ProjectState> GetVisibleProjects(UserContext context)
    {
        if (context.Role != ApplicationRoles.StafMinistrie)
        {
            return _projects;
        }

        return _projects
            .Where(project => project.Ministries.Contains(context.Ministry!, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static string GetPerformanceBucket(int score) => score switch
    {
        >= 85 => PerformanceBuckets.Excellent,
        >= 70 => PerformanceBuckets.Good,
        >= 55 => PerformanceBuckets.NeedsAttention,
        _ => PerformanceBuckets.Critical
    };

    private static string ShortMinistryName(string ministry) => ministry switch
    {
        "Ministria e Infrastrukturës dhe Energjisë" => "M. e Infrastrukturës dhe Energjisë",
        "Ministria e Ekonomisë, Kulturës dhe Inovacionit" => "M. e Ekonomisë, Kulturës dhe Inovacionit",
        "Ministria e Financave" => "M. e Financave",
        "Ministria e Arsimit, Sportit dhe Rinisë" => "M. e Arsimit, Sportit dhe Rinisë",
        "Ministria e Shëndetësisë dhe Mbrojtjes Sociale" => "M. e Shëndetësisë dhe Mbrojtjes Sociale",
        "Ministria e Bujqësisë dhe Zhvillimit Rural" => "M. e Bujqësisë dhe Zhvillimit Rural",
        "Ministria e Drejtësisë" => "M. e Drejtësisë",
        "Ministria e Brendshme" => "M. e Brendshme",
        "Ministria për Evropën dhe Punët e Jashtme" => "M. për Evropën dhe Punët e Jashtme",
        "Ministria e Mbrojtjes" => "M. e Mbrojtjes",
        "Ministria e Turizmit dhe Mjedisit" => "M. e Turizmit dhe Mjedisit",
        "Ministria e Shtetit për Pushtetin Vendor" => "M. e Shtetit për Pushtetin Vendor",
        _ => ministry
    };

    private static int CalculateOkrAverage(ProjectOkr okr) =>
        (int)Math.Round((okr.Deadlines + okr.Quality + okr.Impact + okr.Collaboration) / 4d);

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

    private static ProjectResponse ToResponse(ProjectState project)
    {
        var expectedProgress = CalculateExpectedProgress(project.StartDate, project.EndDate);
        var deviationPercent = expectedProgress - project.Progress;
        var daysRemaining = Math.Max(0, (int)Math.Ceiling((project.EndDate - DateTimeOffset.UtcNow).TotalDays));
        var delayDays = CalculateDelayDays(project);
        var okrAverage = CalculateOkrAverage(project.Okr);

        return new ProjectResponse(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            project.Ministries,
            project.Agency,
            project.Status,
            project.TotalPhases,
            project.CurrentPhase,
            project.StartDate,
            project.EndDate,
            project.Progress,
            expectedProgress,
            deviationPercent,
            daysRemaining,
            delayDays,
            project.Okr,
            project.Risk,
            project.Team,
            project.Lead,
            project.LastUpdated,
            project.UpdateCadenceDays,
            okrAverage,
            IsOverdue(project),
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

    private static AiInsightResponse BuildAiInsights(ProjectState project)
    {
        var response = ToResponse(project);
        var weakest = new Dictionary<string, int>
        {
            ["afatet"] = project.Okr.Deadlines,
            ["cilësia"] = project.Okr.Quality,
            ["impakti"] = project.Okr.Impact,
            ["bashkëpunimi"] = project.Okr.Collaboration
        }.OrderBy(item => item.Value).First();

        var strongest = new Dictionary<string, int>
        {
            ["afatet"] = project.Okr.Deadlines,
            ["cilësia"] = project.Okr.Quality,
            ["impakti"] = project.Okr.Impact,
            ["bashkëpunimi"] = project.Okr.Collaboration
        }.OrderByDescending(item => item.Value).First();

        var concerns = new List<string>();
        var positives = new List<string>();
        var recommendations = new List<string>();

        if (response.DeviationPercent > 10)
        {
            concerns.Add($"Progresi aktual është {response.DeviationPercent}% poshtë ritmit të pritur.");
            recommendations.Add("Riplanifiko fazat që kanë mbetur dhe vendos një pronar të qartë për rikuperimin.");
        }

        if (response.DelayDays > 0)
        {
            concerns.Add($"Projekti llogaritet me rreth {response.DelayDays} ditë vonesë.");
            recommendations.Add("Vendos kontroll javor deri sa devijimi të rikthehet nën 5%.");
        }

        if (response.OkrAverage >= 80)
        {
            positives.Add($"OKR mesatar është {response.OkrAverage}% dhe tregon ekzekutim të qëndrueshëm.");
        }

        positives.Add($"Indikatori më i fortë është {strongest.Key} me {strongest.Value}%.");
        concerns.Add($"Fusha që kërkon më shumë ndërhyrje është {weakest.Key} me {weakest.Value}%.");
        recommendations.Add($"Forco planin e punës për {weakest.Key} në ciklin e ardhshëm javor.");

        var attentionLevel = project.Risk switch
        {
            RiskLevels.Critical => "critical",
            RiskLevels.High => "high",
            _ when response.DeviationPercent > 10 || response.IsOverdue => "medium",
            _ => "normal"
        };

        return new AiInsightResponse(
            project.Id,
            attentionLevel,
            $"Projekti është në statusin {ProjectStatuses.ToLabel(project.Status)} me progres {project.Progress}% dhe OKR mesatar {response.OkrAverage}%. Fokusi kryesor duhet të jetë te {weakest.Key}.",
            $"Risku {RiskLevels.ToLabel(project.Risk).ToLowerInvariant()} lidhet me devijimin aktual prej {response.DeviationPercent}% dhe me dobësinë kryesore te {weakest.Key}.",
            Math.Clamp(65 + (response.DeviationPercent > 8 ? 10 : 0) + (project.Risk is RiskLevels.High or RiskLevels.Critical ? 10 : 0), 60, 95),
            positives,
            concerns.Distinct().ToList(),
            recommendations.Distinct().ToList());
    }

    private static PortfolioMetricsResponse BuildPortfolioMetrics(IReadOnlyCollection<ProjectState> projects)
    {
        if (projects.Count == 0)
        {
            return new PortfolioMetricsResponse(0, 0, 0, 0);
        }

        var averageOkr = (int)Math.Round(projects.Average(project => CalculateOkrAverage(project.Okr)));
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
        new($"end-{project.Id}", project.Id, project.EndDate, EventTypes.Completion, "Mbyllja e projektit")
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
            ["Ministria e Infrastrukturës dhe Energjisë", "Ministria e Ekonomisë, Kulturës dhe Inovacionit"],
            "Agjencia Shtetërore për Shpronësimin",
            ProjectStatuses.Active,
            10,
            7,
            IsoOffset(-220),
            IsoOffset(140),
            70,
            new ProjectOkr(80, 75, 70, 95),
            RiskLevels.Medium,
            ["Erblin Malkurti", "Evilsidio Tosku", "Nensi Ahmetbeja", "Ina Peleshka"],
            "Erblin Malkurti",
            7,
            IsoOffset(-5),
            [
                new ObjectiveState("obj-1", "Përshpejtimi i shpronësimeve", "ASHSH",
                [
                    new KeyResultState("obj-1-kr-1", "Ulja e kohës mesatare të shqyrtimit", 74, 100, "%"),
                    new KeyResultState("obj-1-kr-2", "Digjitalizimi i dosjeve prioritare", 68, 100, "%")
                ])
            ]),
        CreateSampleProject(2, 1, _ministries[0], ProjectStatuses.Planning, 24, RiskLevels.Low, IsoOffset(-2)),
        CreateSampleProject(3, 2, _ministries[1], ProjectStatuses.Active, 33, RiskLevels.High, IsoOffset(-22)),
        CreateSampleProject(4, 3, _ministries[2], ProjectStatuses.Active, 84, RiskLevels.Low, IsoOffset(-3)),
        CreateSampleProject(5, 4, _ministries[3], ProjectStatuses.Blocked, 41, RiskLevels.High, IsoOffset(-16)),
        CreateSampleProject(6, 5, _ministries[4], ProjectStatuses.AtRisk, 58, RiskLevels.Medium, IsoOffset(-16)),
        CreateSampleProject(7, 6, _ministries[5], ProjectStatuses.Completed, 100, RiskLevels.Low, IsoOffset(-19)),
        CreateSampleProject(8, 7, _ministries[6], ProjectStatuses.Cancelled, 12, RiskLevels.Critical, IsoOffset(-28)),
        CreateSampleProject(9, 8, _ministries[7], ProjectStatuses.Active, 63, RiskLevels.Medium, IsoOffset(-6)),
        CreateSampleProject(10, 9, _ministries[8], ProjectStatuses.Planning, 18, RiskLevels.Low, IsoOffset(-4)),
        CreateSampleProject(11, 10, _ministries[9], ProjectStatuses.Active, 52, RiskLevels.Medium, IsoOffset(-7)),
        CreateSampleProject(12, 11, _ministries[10], ProjectStatuses.Active, 47, RiskLevels.High, IsoOffset(-10)),
        CreateSampleProject(13, 12, _ministries[11], ProjectStatuses.Completed, 100, RiskLevels.Low, IsoOffset(-12))
    ];

    private static ProjectState CreateSampleProject(int idNumber, int projectNumber, string ministry, string status, int progress, string risk, DateTimeOffset lastUpdated)
    {
        var totalPhases = status == ProjectStatuses.Completed ? 6 : 8;
        var currentPhase = status == ProjectStatuses.Completed ? totalPhases : Math.Clamp((int)Math.Ceiling(progress / 100d * totalPhases), 1, totalPhases);
        var deadlineScore = Math.Clamp(progress + 18, 35, 95);
        var qualityScore = Math.Clamp(progress + 28, 45, 96);
        var impactScore = Math.Clamp(progress + 24, 45, 97);
        var collaborationScore = Math.Clamp(progress + 20, 40, 94);

        return new ProjectState(
            $"p{idNumber}",
            $"PRJ-{projectNumber:000}",
            $"Projekti {projectNumber}",
            $"Projekt shembull për demonstrim të platformës për {ministry}.",
            [ministry],
            null,
            status,
            totalPhases,
            currentPhase,
            IsoOffset(-90 - (projectNumber * 12)),
            IsoOffset(status == ProjectStatuses.Completed ? -15 : 120 + (projectNumber * 20)),
            progress,
            new ProjectOkr(deadlineScore, qualityScore, impactScore, collaborationScore),
            risk,
            ["Anëtar 1", "Anëtar 2"],
            $"Përgjegjësi {projectNumber}",
            7,
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
            new KeyResultState("portfolio-2-kr-2", "Projektet me KR të përditësuar çdo javë", 68, 90, "%")
        ])
    ];

    private List<WeeklyUpdateState> BuildUpdates() =>
    [
        new("upd-1", "p1", "Drejtori i Inovacionit", ApplicationRoles.DrejtorAgjencie, IsoOffset(-2), 70, ProjectStatuses.Active, new ProjectOkr(80, 75, 70, 95), RiskLevels.Medium, "Koordinimi me dy ministritë kërkon sinkronizim më të shpeshtë.", "Faza 7 po ecën sipas planit, por duhen finalizuar vendimet e ndërmjetme."),
        new("upd-2", "p3", "Ekspert Agjencie", ApplicationRoles.StafAgjencie, IsoOffset(-6), 33, ProjectStatuses.Active, new ProjectOkr(45, 70, 80, 60), RiskLevels.High, "Ka vonesë në miratimin e dokumenteve përgatitore.", "Duhet ndjekje e përditshme me njësinë përkatëse."),
        new("upd-3", "p5", "Ekspert Agjencie", ApplicationRoles.StafAgjencie, IsoOffset(-8), 41, ProjectStatuses.Blocked, new ProjectOkr(56, 60, 64, 58), RiskLevels.High, "Bllokim në furnizim dhe mungesë aprovimesh.", "Kërkohet vendim drejtues për të zhbllokuar varësitë.")
    ];

    private static List<ObjectiveState> BuildSampleObjectives(string prefix, string title) =>
    [
        new(prefix, title, "Drejtoria e Inovacionit",
        [
            new KeyResultState($"{prefix}-kr-1", "KR 1", 62, 100, "%"),
            new KeyResultState($"{prefix}-kr-2", "KR 2", 48, 100, "%")
        ])
    ];

    private sealed class ProjectState(
        string id,
        string code,
        string name,
        string description,
        List<string> ministries,
        string? agency,
        string status,
        int totalPhases,
        int currentPhase,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int progress,
        ProjectOkr okr,
        string risk,
        List<string> team,
        string lead,
        int updateCadenceDays,
        DateTimeOffset lastUpdated,
        List<ObjectiveState> objectives)
    {
        public string Id { get; } = id;
        public string Code { get; } = code;
        public string Name { get; } = name;
        public string Description { get; } = description;
        public List<string> Ministries { get; } = ministries;
        public string? Agency { get; } = agency;
        public string Status { get; set; } = status;
        public int TotalPhases { get; } = totalPhases;
        public int CurrentPhase { get; } = currentPhase;
        public DateTimeOffset StartDate { get; } = startDate;
        public DateTimeOffset EndDate { get; } = endDate;
        public int Progress { get; set; } = progress;
        public ProjectOkr Okr { get; set; } = okr;
        public string Risk { get; set; } = risk;
        public List<string> Team { get; } = team;
        public string Lead { get; } = lead;
        public int UpdateCadenceDays { get; } = updateCadenceDays;
        public DateTimeOffset LastUpdated { get; set; } = lastUpdated;
        public List<ObjectiveState> Objectives { get; } = objectives;
        public int OkrAverage => CalculateOkrAverage(Okr);
        public int ExpectedProgress => CalculateExpectedProgress(StartDate, EndDate);
        public int DelayDays => CalculateDelayDays(this);
        public int DeviationPercent => ExpectedProgress - Progress;
        public int DaysRemaining => Math.Max(0, (int)Math.Ceiling((EndDate - DateTimeOffset.UtcNow).TotalDays));
    }

    private sealed record ObjectiveState(string Id, string Title, string Owner, List<KeyResultState> KeyResults);

    private sealed record KeyResultState(string Id, string Title, int Progress, int Target, string Unit);

    private sealed record WeeklyUpdateState(
        string Id,
        string ProjectId,
        string SubmittedBy,
        string SubmittedRole,
        DateTimeOffset SubmittedAt,
        int Progress,
        string Status,
        ProjectOkr Okr,
        string Risk,
        string Blockers,
        string Comments);

    private sealed record ProjectChangeProposalState(
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
}

internal static class ObjectPipeExtensions
{
    public static TResult Pipe<TSource, TResult>(this TSource source, Func<TSource, TResult> selector) => selector(source);
}






