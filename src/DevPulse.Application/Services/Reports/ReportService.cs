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

            foreach (var entry in developersForAccount)
            {
                if (DemoSeedData.IsDemoWorkspace(account.WorkspaceId))
                {
                    reportTasks.AddRange(
                        DemoReportDataProvider.GetTasksForDateRange(
                            request.FromDate,
                            request.ToDate,
                            account,
                            entry.Developer));
                    continue;
                }

                var mapping = entry.Mapping!;
                var token = _tokenProtector.Unprotect(account.EncryptedAccessToken);
                var query = new ClickUpTaskQueryRequest(
                    account.Id,
                    [mapping.ClickUpUserId],
                    Month: null,
                    request.IncludeClosed,
                    Page: 0,
                    request.FromDate,
                    request.ToDate);

                var tasks = await FetchAllTasksAsync(token, account, query, cancellationToken);

                foreach (var task in tasks)
                {
                    reportTasks.Add(new DeveloperReportTaskDto(
                        entry.Developer.Id,
                        entry.Developer.Name,
                        account.Id,
                        account.Name,
                        task.Id,
                        task.Name,
                        task.Status,
                        task.ListName,
                        task.Url,
                        task.DateCreated,
                        task.DateDone,
                        CalculateCompletionDays(task.DateCreated, task.DateDone)));
                }
            }
        }

        var summaries = activeDevelopers
            .Select(developer => BuildSummary(developer, reportTasks))
            .Where(s => s.TotalTasks > 0 || request.DeveloperIds.Contains(s.DeveloperId))
            .OrderByDescending(s => s.TotalTasks)
            .ThenBy(s => s.DeveloperName)
            .ToList();

        var response = new DeveloperReportResponse(
            request.FromDate,
            request.ToDate,
            reportTasks.Count,
            queriedWorkspaces.Count,
            summaries,
            reportTasks.OrderBy(t => t.DeveloperName).ThenBy(t => t.AccountName).ThenBy(t => t.TaskName).ToList());

        _logger.LogInformation(
            "Generated developer report for {FromDate} to {ToDate}: {TaskCount} tasks across {WorkspaceCount} workspaces",
            request.FromDate,
            request.ToDate,
            response.TotalTasksCompleted,
            response.WorkspaceCount);

        return Result<DeveloperReportResponse>.Success(response);
    }

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

    private static DeveloperReportSummaryDto BuildSummary(Developer developer, IReadOnlyList<DeveloperReportTaskDto> tasks)
    {
        var developerTasks = tasks.Where(t => t.DeveloperId == developer.Id).ToList();
        var byWorkspace = developerTasks
            .GroupBy(t => new { t.AccountId, t.AccountName })
            .Select(g => new DeveloperWorkspaceBreakdownDto(g.Key.AccountId, g.Key.AccountName, g.Count()))
            .OrderByDescending(x => x.TaskCount)
            .ToList();

        var completionDays = developerTasks
            .Where(t => t.CompletionDays.HasValue)
            .Select(t => t.CompletionDays!.Value)
            .ToList();

        return new DeveloperReportSummaryDto(
            developer.Id,
            developer.Name,
            developer.Email,
            developerTasks.Count,
            byWorkspace.Count,
            completionDays.Count > 0 ? Math.Round(completionDays.Average(), 1) : null,
            byWorkspace);
    }

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
