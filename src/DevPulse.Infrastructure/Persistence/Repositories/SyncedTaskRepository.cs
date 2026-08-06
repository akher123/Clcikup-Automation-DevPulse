using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevPulse.Infrastructure.Persistence.Repositories;

public sealed class SyncedTaskRepository : ISyncedTaskRepository
{
    private readonly DevPulseDbContext _dbContext;

    public SyncedTaskRepository(DevPulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertRangeAsync(IReadOnlyList<SyncedTask> tasks, CancellationToken cancellationToken = default)
    {
        if (tasks.Count == 0)
        {
            return;
        }

        var accountIds = tasks.Select(t => t.AccountId).Distinct().ToList();
        var taskIds = tasks.Select(t => t.TaskId).Distinct().ToList();

        var existing = await _dbContext.SyncedTasks
            .Where(t => accountIds.Contains(t.AccountId) && taskIds.Contains(t.TaskId))
            .ToListAsync(cancellationToken);

        var existingByKey = existing.ToDictionary(
            t => (t.DeveloperId, t.AccountId, t.TaskId),
            t => t);

        foreach (var incoming in tasks)
        {
            var key = (incoming.DeveloperId, incoming.AccountId, incoming.TaskId);
            if (existingByKey.TryGetValue(key, out var current))
            {
                current.AccountName = incoming.AccountName;
                current.ProjectName = incoming.ProjectName;
                current.FolderName = incoming.FolderName;
                current.TaskName = incoming.TaskName;
                current.Status = incoming.Status;
                current.Priority = incoming.Priority;
                current.ListName = incoming.ListName;
                current.Url = incoming.Url;
                current.DateCreated = incoming.DateCreated;
                current.DateDone = incoming.DateDone;
                current.DueDate = incoming.DueDate;
                current.CompletionDays = incoming.CompletionDays;
                current.IsSubtask = incoming.IsSubtask;
                current.ParentTaskId = incoming.ParentTaskId;
                current.ParentTaskName = incoming.ParentTaskName;
                current.TaskType = incoming.TaskType;
                current.IsCompleted = incoming.IsCompleted;
                current.SyncedAtUtc = incoming.SyncedAtUtc;
            }
            else
            {
                _dbContext.SyncedTasks.Add(incoming);
                existingByKey[key] = incoming;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SyncedTask>> GetForReportAsync(
        IReadOnlyList<Guid> developerIds,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyList<Guid>? accountIds = null,
        CancellationToken cancellationToken = default)
    {
        var fromMs = ToRangeStartMs(fromDate);
        var toExclusiveMs = ToRangeEndExclusiveMs(toDate);

        var query = _dbContext.SyncedTasks
            .AsNoTracking()
            .Where(t => developerIds.Contains(t.DeveloperId));

        if (accountIds is { Count: > 0 })
        {
            query = query.Where(t => accountIds.Contains(t.AccountId));
        }

        // Match live report semantics: completed by dateDone, open by dateCreated.
        query = query.Where(t =>
            (t.IsCompleted && t.DateDone.HasValue && t.DateDone.Value >= fromMs && t.DateDone.Value < toExclusiveMs)
            || (!t.IsCompleted && t.DateCreated.HasValue && t.DateCreated.Value >= fromMs && t.DateCreated.Value < toExclusiveMs));

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<DateTime?> GetLatestSyncedAtAsync(CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.SyncedTasks.AsNoTracking().AnyAsync(cancellationToken))
        {
            return null;
        }

        return await _dbContext.SyncedTasks
            .AsNoTracking()
            .MaxAsync(t => t.SyncedAtUtc, cancellationToken);
    }

    private static long ToRangeStartMs(DateOnly fromDate) =>
        new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

    private static long ToRangeEndExclusiveMs(DateOnly toDate) =>
        new DateTimeOffset(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
}
