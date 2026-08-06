using DevPulse.Domain.Entities;

namespace DevPulse.Application.Abstractions.Persistence;

public interface ISyncedTaskRepository
{
    Task UpsertRangeAsync(IReadOnlyList<SyncedTask> tasks, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SyncedTask>> GetForReportAsync(
        IReadOnlyList<Guid> developerIds,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyList<Guid>? accountIds = null,
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetLatestSyncedAtAsync(CancellationToken cancellationToken = default);
}
