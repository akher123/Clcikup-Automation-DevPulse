using DevPulse.Shared.Contracts.ClickUp;

namespace DevPulse.Application.Abstractions.ClickUp;

public interface IClickUpApiClient
{
    Task<IReadOnlyList<ClickUpWorkspaceDto>> GetAuthorizedWorkspacesAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClickUpMemberDto>> GetWorkspaceMembersAsync(
        string accessToken,
        string workspaceId,
        CancellationToken cancellationToken = default);

    Task<ClickUpTaskQueryResponse> GetFilteredTasksAsync(
        string accessToken,
        string workspaceId,
        string accountName,
        Guid accountId,
        ClickUpTaskQueryRequest query,
        CancellationToken cancellationToken = default);
}
