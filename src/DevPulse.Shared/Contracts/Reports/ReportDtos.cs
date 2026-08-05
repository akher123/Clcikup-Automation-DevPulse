using System.Text.Json.Serialization;

namespace DevPulse.Shared.Contracts.Reports;

public record DeveloperReportRequest(
    [property: JsonPropertyName("developerIds")] IReadOnlyList<Guid> DeveloperIds,
    [property: JsonPropertyName("fromDate")] DateOnly FromDate,
    [property: JsonPropertyName("toDate")] DateOnly ToDate,
    [property: JsonPropertyName("includeClosed")] bool IncludeClosed = true,
    [property: JsonPropertyName("accountIds")] IReadOnlyList<Guid>? AccountIds = null);

public record DeveloperReportResponse(
    [property: JsonPropertyName("fromDate")] DateOnly FromDate,
    [property: JsonPropertyName("toDate")] DateOnly ToDate,
    [property: JsonPropertyName("totalTasksCompleted")] int TotalTasksCompleted,
    [property: JsonPropertyName("workspaceCount")] int WorkspaceCount,
    [property: JsonPropertyName("developers")] IReadOnlyList<DeveloperReportSummaryDto> Developers,
    [property: JsonPropertyName("tasks")] IReadOnlyList<DeveloperReportTaskDto> Tasks);

public record DeveloperReportSummaryDto(
    [property: JsonPropertyName("developerId")] Guid DeveloperId,
    [property: JsonPropertyName("developerName")] string DeveloperName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("totalTasks")] int TotalTasks,
    [property: JsonPropertyName("workspaceCount")] int WorkspaceCount,
    [property: JsonPropertyName("averageCompletionDays")] double? AverageCompletionDays,
    [property: JsonPropertyName("byWorkspace")] IReadOnlyList<DeveloperWorkspaceBreakdownDto> ByWorkspace);

public record DeveloperWorkspaceBreakdownDto(
    [property: JsonPropertyName("accountId")] Guid AccountId,
    [property: JsonPropertyName("accountName")] string AccountName,
    [property: JsonPropertyName("taskCount")] int TaskCount);

public record DeveloperReportTaskDto(
    [property: JsonPropertyName("developerId")] Guid DeveloperId,
    [property: JsonPropertyName("developerName")] string DeveloperName,
    [property: JsonPropertyName("accountId")] Guid AccountId,
    [property: JsonPropertyName("accountName")] string AccountName,
    [property: JsonPropertyName("taskId")] string TaskId,
    [property: JsonPropertyName("taskName")] string TaskName,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("listName")] string? ListName,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("dateCreated")] long? DateCreated,
    [property: JsonPropertyName("dateDone")] long? DateDone,
    [property: JsonPropertyName("completionDays")] double? CompletionDays);
