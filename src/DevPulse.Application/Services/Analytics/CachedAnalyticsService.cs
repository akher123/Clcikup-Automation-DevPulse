using DevPulse.Application.Abstractions.Analytics;
using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Shared.Common;
using DevPulse.Shared.Contracts.Analytics;
using DevPulse.Shared.Contracts.Reports;
using Microsoft.Extensions.Logging;

namespace DevPulse.Application.Services.Analytics;

public sealed class CachedAnalyticsService : ICachedAnalyticsService
{
    private readonly ISyncedTaskRepository _syncedTaskRepository;
    private readonly IDeveloperRepository _developerRepository;
    private readonly IKpiSyncRunRepository _syncRunRepository;
    private readonly ILogger<CachedAnalyticsService> _logger;

    public CachedAnalyticsService(
        ISyncedTaskRepository syncedTaskRepository,
        IDeveloperRepository developerRepository,
        IKpiSyncRunRepository syncRunRepository,
        ILogger<CachedAnalyticsService> logger)
    {
        _syncedTaskRepository = syncedTaskRepository;
        _developerRepository = developerRepository;
        _syncRunRepository = syncRunRepository;
        _logger = logger;
    }

    public async Task<Result<CachedAnalyticsResponse>> GetAnalyticsFromDatabaseAsync(
        CachedAnalyticsRequest request,
        CancellationToken cancellationToken = default)
    {
        var reportResult = await GenerateReportFromDatabaseAsync(
            new DeveloperReportRequest(
                request.DeveloperIds,
                request.FromDate,
                request.ToDate,
                IncludeClosed: true,
                request.AccountIds),
            cancellationToken);

        if (!reportResult.IsSuccess || reportResult.Value is null)
        {
            return Result<CachedAnalyticsResponse>.Failure(reportResult.Error ?? "Failed to load analytics from database.");
        }

        var lastRun = await _syncRunRepository.GetLatestAsync(cancellationToken);
        var syncedAt = await _syncedTaskRepository.GetLatestSyncedAtAsync(cancellationToken);

        return Result<CachedAnalyticsResponse>.Success(new CachedAnalyticsResponse(
            reportResult.Value,
            "database",
            syncedAt,
            lastRun is null ? null : KpiSyncService.ToDto(lastRun)));
    }

    public async Task<Result<DeveloperReportResponse>> GenerateReportFromDatabaseAsync(
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

        var syncedTasks = await _syncedTaskRepository.GetForReportAsync(
            activeDevelopers.Select(d => d.Id).ToList(),
            request.FromDate,
            request.ToDate,
            request.AccountIds,
            cancellationToken);

        if (syncedTasks.Count == 0)
        {
            var lastRun = await _syncRunRepository.GetLatestAsync(cancellationToken);
            var hint = lastRun is null
                ? "No synced KPI data found. Run a KPI sync first."
                : $"No synced tasks match this period. Last sync covered {lastRun.FromDate:yyyy-MM-dd} to {lastRun.ToDate:yyyy-MM-dd} ({lastRun.Status}).";
            return Result<DeveloperReportResponse>.Failure(hint);
        }

        var nameById = activeDevelopers.ToDictionary(d => d.Id, d => d.Name);
        var emailById = activeDevelopers.ToDictionary(d => d.Id, d => d.Email);

        var reportTasks = syncedTasks
            .Select(t => ReportTaskMapper.ToReportTask(t, nameById.GetValueOrDefault(t.DeveloperId, "Unknown")))
            .GroupBy(t => (t.DeveloperId, t.AccountId, t.TaskId))
            .Select(g => g.OrderByDescending(t => t.IsCompleted).First())
            .OrderBy(t => t.DeveloperName)
            .ThenBy(t => t.IsCompleted)
            .ThenBy(t => t.AccountName)
            .ThenBy(t => t.ProjectName)
            .ThenBy(t => t.TaskName)
            .ToList();

        var summaries = activeDevelopers
            .Select(d => ReportTaskMapper.BuildSummary(
                d.Id,
                d.Name,
                emailById.GetValueOrDefault(d.Id),
                reportTasks))
            .Where(s => s.TotalTasks > 0 || request.DeveloperIds.Contains(s.DeveloperId))
            .OrderByDescending(s => s.TotalTasks)
            .ThenBy(s => s.DeveloperName)
            .ToList();

        var workspaceCount = reportTasks.Select(t => t.AccountId).Distinct().Count();
        var response = new DeveloperReportResponse(
            request.FromDate,
            request.ToDate,
            reportTasks.Count(t => t.IsCompleted),
            reportTasks.Count(t => !t.IsCompleted),
            workspaceCount,
            summaries,
            reportTasks);

        _logger.LogInformation(
            "Generated DB-backed report for {FromDate}–{ToDate}: {Completed} completed, {InProgress} in progress from {TaskCount} synced tasks",
            request.FromDate,
            request.ToDate,
            response.TotalTasksCompleted,
            response.TotalInProgress,
            reportTasks.Count);

        return Result<DeveloperReportResponse>.Success(response);
    }
}
