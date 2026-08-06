using DevPulse.Domain.Entities;
using DevPulse.Shared.Contracts.Reports;

namespace DevPulse.Application.Services.Analytics;

internal static class ReportTaskMapper
{
    public static SyncedTask ToSyncedTask(DeveloperReportTaskDto task, DateTime syncedAtUtc) =>
        new()
        {
            DeveloperId = task.DeveloperId,
            AccountId = task.AccountId,
            AccountName = task.AccountName,
            ProjectName = task.ProjectName,
            FolderName = task.FolderName,
            TaskId = task.TaskId,
            TaskName = task.TaskName,
            Status = task.Status,
            Priority = task.Priority,
            ListName = task.ListName,
            Url = task.Url,
            DateCreated = task.DateCreated,
            DateDone = task.DateDone,
            DueDate = task.DueDate,
            CompletionDays = task.CompletionDays,
            IsSubtask = task.IsSubtask,
            ParentTaskId = task.ParentTaskId,
            ParentTaskName = task.ParentTaskName,
            TaskType = task.TaskType,
            IsCompleted = task.IsCompleted,
            SyncedAtUtc = syncedAtUtc
        };

    public static DeveloperReportTaskDto ToReportTask(SyncedTask task, string developerName) =>
        new(
            task.DeveloperId,
            developerName,
            task.AccountId,
            task.AccountName,
            task.ProjectName,
            task.FolderName,
            task.TaskId,
            task.TaskName,
            task.Status,
            task.Priority,
            task.ListName,
            task.Url,
            task.DateCreated,
            task.DateDone,
            task.DueDate,
            task.CompletionDays,
            task.IsSubtask,
            task.ParentTaskId,
            task.ParentTaskName,
            task.TaskType,
            task.IsCompleted);

    public static DeveloperReportSummaryDto BuildSummary(
        Guid developerId,
        string developerName,
        string? email,
        IReadOnlyList<DeveloperReportTaskDto> tasks)
    {
        var developerTasks = tasks.Where(t => t.DeveloperId == developerId).ToList();
        var byWorkspace = developerTasks
            .GroupBy(t => new { t.AccountId, t.AccountName })
            .Select(g => new DeveloperWorkspaceBreakdownDto(g.Key.AccountId, g.Key.AccountName, g.Count()))
            .OrderByDescending(x => x.TaskCount)
            .ToList();

        var byProject = developerTasks
            .GroupBy(t => new { t.AccountId, t.AccountName, ProjectName = t.ProjectName ?? "Unknown" })
            .Select(g => new DeveloperProjectBreakdownDto(g.Key.AccountId, g.Key.AccountName, g.Key.ProjectName, g.Count()))
            .OrderByDescending(x => x.TaskCount)
            .ToList();

        var completionDays = developerTasks
            .Where(t => t.IsCompleted && t.CompletionDays.HasValue)
            .Select(t => t.CompletionDays!.Value)
            .ToList();

        return new DeveloperReportSummaryDto(
            developerId,
            developerName,
            email,
            developerTasks.Count,
            developerTasks.Count(t => t.IsCompleted),
            developerTasks.Count(t => !t.IsCompleted),
            developerTasks.Count(t => t.IsSubtask),
            byWorkspace.Count,
            byProject.Count,
            developerTasks.Count(IsOverdue),
            developerTasks.Count(IsOnTimeCompletion),
            completionDays.Count > 0 ? Math.Round(completionDays.Average(), 1) : null,
            byWorkspace,
            byProject);
    }

    public static double Rate(int numerator, int denominator) =>
        denominator <= 0 ? 0 : Math.Round(numerator * 100.0 / denominator, 1);

    public static double? DeliveryHealth(int onTimeCompleted, int overdueCount)
    {
        var denominator = onTimeCompleted + overdueCount;
        return denominator > 0 ? Rate(onTimeCompleted, denominator) : null;
    }

    private static bool IsOverdue(DeveloperReportTaskDto task)
    {
        if (!task.DueDate.HasValue)
        {
            return false;
        }

        if (task.IsCompleted)
        {
            return task.DateDone.HasValue && task.DateDone.Value > task.DueDate.Value;
        }

        return task.DueDate.Value < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private static bool IsOnTimeCompletion(DeveloperReportTaskDto task) =>
        task.IsCompleted
        && task.DueDate.HasValue
        && task.DateDone.HasValue
        && task.DateDone.Value <= task.DueDate.Value;
}
