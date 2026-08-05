using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Infrastructure.ClickUp.Models;
using DevPulse.Shared.Contracts.ClickUp;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.ClickUp;

/// <summary>
/// Typed HTTP client for ClickUp API v2.
/// Uses token-per-request pattern to support multiple accounts with one client instance.
/// </summary>
public sealed class ClickUpApiClient : IClickUpApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClickUpApiClient> _logger;

    public ClickUpApiClient(HttpClient httpClient, ILogger<ClickUpApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ClickUpWorkspaceDto>> GetAuthorizedWorkspacesAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<ClickUpTeamsResponse>(HttpMethod.Get, "team", accessToken, cancellationToken);
        return response.Teams
            .Select(t => new ClickUpWorkspaceDto(t.Id, t.Name, t.Color))
            .ToList();
    }

    public async Task<IReadOnlyList<ClickUpMemberDto>> GetWorkspaceMembersAsync(
        string accessToken,
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<ClickUpMembersResponse>(
            HttpMethod.Get,
            $"team/{workspaceId}/member",
            accessToken,
            cancellationToken);

        return response.Members
            .Select(m => new ClickUpMemberDto(
                m.User.Id,
                m.User.Username,
                m.User.Email,
                m.User.ProfilePicture))
            .ToList();
    }

    public async Task<ClickUpTaskQueryResponse> GetFilteredTasksAsync(
        string accessToken,
        string workspaceId,
        string accountName,
        Guid accountId,
        ClickUpTaskQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var queryString = BuildTaskQueryString(query);
        var response = await SendAsync<ClickUpTasksResponse>(
            HttpMethod.Get,
            $"team/{workspaceId}/task?{queryString}",
            accessToken,
            cancellationToken);

        var tasks = response.Tasks.Select(t => new ClickUpTaskDto(
            t.Id,
            t.Name,
            t.Status?.Status,
            accountName,
            t.List?.Name,
            t.Url,
            t.DateCreated,
            t.DateClosed,
            t.DueDate,
            t.Assignees?.Select(a => a.Email ?? a.Username).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? []))
            .ToList();

        return new ClickUpTaskQueryResponse(accountId, accountName, query.Page, tasks.Count, tasks);
    }

    private static string BuildTaskQueryString(ClickUpTaskQueryRequest query)
    {
        var parts = new List<string>
        {
            $"page={query.Page}",
            $"include_closed={(query.IncludeClosed ? "true" : "false")}"
        };

        if (query.Month.HasValue)
        {
            var start = new DateTimeOffset(query.Month.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
            var end = new DateTimeOffset(query.Month.Value.AddMonths(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
            parts.Add($"date_done_gt={start}");
            parts.Add($"date_done_lt={end}");
        }

        if (query.AssigneeIds is not null)
        {
            parts.AddRange(query.AssigneeIds.Select(id => $"assignees[]={id}"));
        }

        return string.Join('&', parts);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string relativeUrl,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("ClickUp rate limit reached for {Url}", relativeUrl);
            throw new HttpRequestException("ClickUp API rate limit exceeded.", null, HttpStatusCode.TooManyRequests);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("ClickUp API error {StatusCode} for {Url}: {Body}", (int)response.StatusCode, relativeUrl, body);
            throw new HttpRequestException($"ClickUp API returned {(int)response.StatusCode}.", null, response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return payload ?? throw new InvalidOperationException("ClickUp API returned an empty response.");
    }
}
