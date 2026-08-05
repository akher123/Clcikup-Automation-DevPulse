using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Application.Abstractions.Reports;
using DevPulse.Application.Abstractions.Security;
using DevPulse.Domain.Entities;
using DevPulse.Shared.Common;
using DevPulse.Shared.Constants;
using DevPulse.Shared.Contracts.ClickUp;
using DevPulse.Shared.Contracts.Reports;
using Microsoft.Extensions.Logging;

namespace DevPulse.Application.Services.Reports;

public sealed class ReportService : IReportService
{
    private const int ClickUpPageSize = 100;

    private readonly IDeveloperRepository _developerRepository;
    private readonly IClickUpAccountRepository _accountRepository;
    private readonly IClickUpApiClient _apiClient;
    private readonly ITokenProtector _tokenProtector;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IDeveloperRepository developerRepository,
        IClickUpAccountRepository accountRepository,
        IClickUpApiClient apiClient,
        ITokenProtector tokenProtector,
        ILogger<ReportService> logger)
    {
        _developerRepository = developerRepository;
        _accountRepository = accountRepository;
        _apiClient = apiClient;
        _tokenProtector = tokenProtector;
        _logger = logger;
    }

    public async Task<Result<DeveloperReportResponse>> GenerateDeveloperReportAsync(
        DeveloperReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.DeveloperIds.Count == 0)
        {
            return Result<DeveloperReportResponse>.Failure("Select at least one developer.");
        }

        if (request.FromDate > request.ToDate)
        {
            return Result<DeveloperReportResponse>.Failure("From date must be on or before to date.");
        }

        var developers = await _developerRepository.GetByIdsWithMappingsAsync(request.DeveloperIds, cancellationToken);
        if (developers.Count == 0)
        {
            return Result<DeveloperReportResponse>.Failure("No matching developers were found.");
        }

        var activeDevelopers = developers.Where(d => d.IsActive).ToList();
        if (activeDevelopers.Count == 0)
        {
            return Result<DeveloperReportResponse>.Failure("Selected developers are inactive.");
        }

        var accounts = await _accountRepository.GetActiveAsync(cancellationToken);
        if (accounts.Count == 0)
        {
            return Result<DeveloperReportResponse>.Failure("No active ClickUp accounts configured.");
        }

        if (request.AccountIds is { Count: > 0 })
        {
            accounts = accounts.Where(a => request.AccountIds.Contains(a.Id)).ToList();
            if (accounts.Count == 0)
            {
                return Result<DeveloperReportResponse>.Failure("No matching ClickUp accounts were found.");
            }
        }

        var reportTasks = new List<DeveloperReportTaskDto>();
        var queriedWorkspaces = new HashSet<Guid>();

        foreach (var account in accounts)
        {
            var developersForAccount = activeDevelopers
                .Select(d => new
                {
                    Developer = d,
                    Mapping = d.ClickUpMappings.FirstOrDefault(m => m.ClickUpAccountId == account.Id)
                })
                .Where(x => x.Mapping is not null)
                .ToList();

            if (developersForAccount.Count == 0)
            {
                continue;
            }

            queriedWorkspaces.Add(account.Id);

            string token = string.Empty;
            if (!DemoSeedData.IsDemoWorkspace(account.WorkspaceId))
            {
                token = _tokenProtector.Unprotect(account.EncryptedAccessToken);
            }

            foreach (var entry in developersForAccount)
            {
                if (DemoSeedData.IsDemoWorkspace(account.WorkspaceId))
                {
                    reportTasks.AddRange(
                        DemoReportDataProvider.GetTasksForDateRange(
                            request.FromDate,
                            request.ToDate,
                            account,
                            entry.Developer)
                            .Where(IsTaskOrSubtask));
                    continue;
                }

                var mapping = entry.Mapping!;
                var developerTasks = await FetchDeveloperTasksAsync(
                    token,
                    account,
                    mapping.ClickUpUserId,
                    request,
                    cancellationToken);

                reportTasks.AddRange(developerTasks.Select(task => ToReportTask(entry.Developer, account, task)));
            }
        }

        // Prefer completed row when the same task appears in both queries.
        reportTasks = reportTasks
            .GroupBy(t => (t.DeveloperId, t.AccountId, t.TaskId))
            .Select(g => g.OrderByDescending(t => t.IsCompleted).First())
            .ToList();

        var summaries = activeDevelopers
            .Select(developer => BuildSummary(developer, reportTasks))
            .Where(s => s.TotalTasks > 0 || request.DeveloperIds.Contains(s.DeveloperId))
            .OrderByDescending(s => s.TotalTasks)
            .ThenBy(s => s.DeveloperName)
            .ToList();

        var completedCount = reportTasks.Count(t => t.IsCompleted);
        var inProgressCount = reportTasks.Count(t => !t.IsCompleted);

        var response = new DeveloperReportResponse(
            request.FromDate,
            request.ToDate,
            completedCount,
            inProgressCount,
            queriedWorkspaces.Count,
            summaries,
            reportTasks
                .OrderBy(t => t.DeveloperName)
                .ThenBy(t => t.IsCompleted)
                .ThenBy(t => t.AccountName)
                .ThenBy(t => t.TaskName)
                .ToList());

        _logger.LogInformation(
            "Generated developer report for {FromDate} to {ToDate}: {CompletedCount} completed, {InProgressCount} in progress across {WorkspaceCount} workspaces",
            request.FromDate,
            request.ToDate,
            response.TotalTasksCompleted,
            response.TotalInProgress,
            response.WorkspaceCount);

        return Result<DeveloperReportResponse>.Success(response);
    }

    private async Task<IReadOnlyList<ClickUpTaskDto>> FetchDeveloperTasksAsync(
        string token,
        ClickUpAccount account,
        int clickUpUserId,
        DeveloperReportRequest request,
        CancellationToken cancellationToken)
    {
        var fromMs = ToRangeStartMs(request.FromDate);
        var toExclusiveMs = ToRangeEndExclusiveMs(request.ToDate);

        // Completed tasks done within the selected date range.
        var completedQuery = new ClickUpTaskQueryRequest(
            account.Id,
            [clickUpUserId],
            Month: null,
            request.IncludeClosed,
            Page: 0,
            request.FromDate,
            request.ToDate,
            IncludeSubtasks: true,
            CustomItemIds: [0],
            DateFilter: ClickUpDateFilterMode.DateDone);

        // Open / in-progress tasks created within the selected date range.
        var openQuery = new ClickUpTaskQueryRequest(
            account.Id,
            [clickUpUserId],
            Month: null,
            IncludeClosed: false,
            Page: 0,
            request.FromDate,
            request.ToDate,
            IncludeSubtasks: true,
            CustomItemIds: [0],
            DateFilter: ClickUpDateFilterMode.DateCreated);

        var completed = await FetchAllTasksAsync(token, account, completedQuery, cancellationToken);
        var open = await FetchAllTasksAsync(token, account, openQuery, cancellationToken);

        return completed
            .Concat(open)
            .Where(t => !IsBugCustomItem(t.CustomItemId, t.TaskTypeName))
            .Where(t => IsWithinSelectedDateRange(t, fromMs, toExclusiveMs))
            .GroupBy(t => t.Id, StringComparer.Ordinal)
            .Select(g => g.FirstOrDefault(t => IsCompletedTask(t)) ?? g.First())
            .ToList();
    }

    private static bool IsWithinSelectedDateRange(ClickUpTaskDto task, long fromMs, long toExclusiveMs)
    {
        if (IsCompletedTask(task))
        {
            return task.DateDone.HasValue
                && task.DateDone.Value >= fromMs
                && task.DateDone.Value < toExclusiveMs;
        }

        // In-progress items must have been created inside the selected range.
        return task.DateCreated.HasValue
            && task.DateCreated.Value >= fromMs
            && task.DateCreated.Value < toExclusiveMs;
    }

    private static long ToRangeStartMs(DateOnly fromDate) =>
        new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

    private static long ToRangeEndExclusiveMs(DateOnly toDate) =>
        new DateTimeOffset(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

    private async Task<IReadOnlyList<ClickUpTaskDto>> FetchAllTasksAsync(
        string token,
        ClickUpAccount account,
        ClickUpTaskQueryRequest baseQuery,
        CancellationToken cancellationToken)
    {
        var allTasks = new List<ClickUpTaskDto>();
        var page = 0;

        while (true)
        {
            var query = baseQuery with { Page = page };
            var response = await _apiClient.GetFilteredTasksAsync(
                token,
                account.WorkspaceId,
                account.Name,
                account.Id,
                query,
                cancellationToken);

            allTasks.AddRange(response.Tasks);

            if (response.Tasks.Count < ClickUpPageSize)
            {
                break;
            }

            page++;
        }

        return allTasks;
    }

    private static DeveloperReportTaskDto ToReportTask(Developer developer, ClickUpAccount account, ClickUpTaskDto task)
    {
        var isCompleted = IsCompletedTask(task);
        return new DeveloperReportTaskDto(
            developer.Id,
            developer.Name,
            account.Id,
            account.Name,
            task.Id,
            task.Name,
            task.Status,
            task.ListName,
            task.Url,
            task.DateCreated,
            task.DateDone,
            isCompleted ? CalculateCompletionDays(task.DateCreated, task.DateDone) : null,
            task.IsSubtask,
            task.ParentTaskId,
            task.IsSubtask ? "Subtask" : "Task",
            isCompleted);
    }

    private static DeveloperReportSummaryDto BuildSummary(Developer developer, IReadOnlyList<DeveloperReportTaskDto> tasks)
    {
        var developerTasks = tasks.Where(t => t.DeveloperId == developer.Id).ToList();
        var byWorkspace = developerTasks
            .GroupBy(t => new { t.AccountId, t.AccountName })
            .Select(g => new DeveloperWorkspaceBreakdownDto(g.Key.AccountId, g.Key.AccountName, g.Count()))
            .OrderByDescending(x => x.TaskCount)
            .ToList();

        var completionDays = developerTasks
            .Where(t => t.IsCompleted && t.CompletionDays.HasValue)
            .Select(t => t.CompletionDays!.Value)
            .ToList();

        return new DeveloperReportSummaryDto(
            developer.Id,
            developer.Name,
            developer.Email,
            developerTasks.Count,
            developerTasks.Count(t => t.IsCompleted),
            developerTasks.Count(t => !t.IsCompleted),
            developerTasks.Count(t => t.IsSubtask),
            byWorkspace.Count,
            completionDays.Count > 0 ? Math.Round(completionDays.Average(), 1) : null,
            byWorkspace);
    }

    private static bool IsCompletedTask(ClickUpTaskDto task)
    {
        if (task.DateDone.HasValue)
        {
            return true;
        }

        return IsCompletedStatus(task.Status);
    }

    private static bool IsCompletedStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status.Trim();
        return normalized.Equals("complete", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("completed", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("closed", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("done", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTaskOrSubtask(DeveloperReportTaskDto task) =>
        !task.TaskType.Contains("bug", StringComparison.OrdinalIgnoreCase);

    private static bool IsBugCustomItem(int? customItemId, string taskTypeName) =>
        customItemId is > 0 ||
        taskTypeName.Contains("bug", StringComparison.OrdinalIgnoreCase);

    private static double? CalculateCompletionDays(long? dateCreated, long? dateDone)
    {
        if (!dateCreated.HasValue || !dateDone.HasValue)
        {
            return null;
        }

        var created = DateTimeOffset.FromUnixTimeMilliseconds(dateCreated.Value);
        var done = DateTimeOffset.FromUnixTimeMilliseconds(dateDone.Value);
        return Math.Round((done - created).TotalDays, 1);
    }
}
