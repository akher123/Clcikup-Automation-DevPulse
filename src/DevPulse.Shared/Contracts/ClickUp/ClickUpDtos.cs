namespace DevPulse.Shared.Contracts.ClickUp;

public record ClickUpAccountDto(
    Guid Id,
    string Name,
    string WorkspaceId,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastValidatedAtUtc,
    string? LastValidationMessage);

public record CreateClickUpAccountRequest(
    string Name,
    string WorkspaceId,
    string AccessToken);

public record UpdateClickUpAccountRequest(
    string Name,
    string WorkspaceId,
    string? AccessToken);

public record ClickUpConnectionTestDto(
    Guid AccountId,
    string AccountName,
    bool IsConnected,
    string Status,
    string? WorkspaceName,
    string Message);

public record ClickUpMemberDto(
    int ClickUpUserId,
    string Username,
    string? Email,
    string? ProfilePicture);

public record ClickUpUserLookupDto(
    int ClickUpUserId,
    string Username,
    string? Email,
    string? ProfilePicture,
    string WorkspaceId,
    Guid AccountId,
    string AccountName);

public record ClickUpWorkspaceDto(
    string Id,
    string Name,
    string? Color);

public record ClickUpTaskDto(
    string Id,
    string Name,
    string? Status,
    string? WorkspaceName,
    string? ListName,
    string? Url,
    long? DateCreated,
    long? DateDone,
    long? DueDate,
    IReadOnlyList<string> AssigneeEmails);

public record ClickUpTaskQueryRequest(
    Guid AccountId,
    IReadOnlyList<int>? AssigneeIds,
    DateOnly? Month,
    bool IncludeClosed = true,
    int Page = 0,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);

public record ClickUpTaskQueryResponse(
    Guid AccountId,
    string AccountName,
    int Page,
    int TaskCount,
    IReadOnlyList<ClickUpTaskDto> Tasks);
