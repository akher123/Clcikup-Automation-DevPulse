using System.Net.Http.Json;
using DevPulse.Shared.Contracts.ClickUp;

namespace DevPulse.Client.Services;

public interface IClickUpAccountApiClient
{
    Task<IReadOnlyList<ClickUpAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);

    Task<ClickUpAccountDto?> CreateAccountAsync(CreateClickUpAccountRequest request, CancellationToken cancellationToken = default);

    Task<ClickUpConnectionTestDto?> TestConnectionAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClickUpMemberDto>> GetMembersAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task DeleteAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public sealed class ClickUpAccountApiClient : IClickUpAccountApiClient
{
    private readonly HttpClient _httpClient;

    public ClickUpAccountApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ClickUpAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _httpClient.GetFromJsonAsync<List<ClickUpAccountDto>>("api/clickup/accounts", cancellationToken);
        return accounts ?? [];
    }

    public async Task<ClickUpAccountDto?> CreateAccountAsync(CreateClickUpAccountRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/clickup/accounts", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to create account: {error}");
        }

        return await response.Content.ReadFromJsonAsync<ClickUpAccountDto>(cancellationToken);
    }

    public async Task<ClickUpConnectionTestDto?> TestConnectionAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ClickUpConnectionTestDto>(
            $"api/clickup/accounts/{accountId}/test",
            cancellationToken);
    }

    public async Task<IReadOnlyList<ClickUpMemberDto>> GetMembersAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var members = await _httpClient.GetFromJsonAsync<List<ClickUpMemberDto>>(
            $"api/clickup/accounts/{accountId}/members",
            cancellationToken);

        return members ?? [];
    }

    public async Task DeleteAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/clickup/accounts/{accountId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
