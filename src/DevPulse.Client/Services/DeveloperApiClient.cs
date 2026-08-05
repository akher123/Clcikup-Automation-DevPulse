using System.Net.Http.Json;
using DevPulse.Shared.Contracts.Developers;

namespace DevPulse.Client.Services;

public interface IDeveloperApiClient
{
    Task<IReadOnlyList<DeveloperDto>> GetDevelopersAsync(CancellationToken cancellationToken = default);

    Task<DeveloperDto?> CreateDeveloperAsync(CreateDeveloperRequest request, CancellationToken cancellationToken = default);

    Task<DeveloperDto?> UpdateDeveloperAsync(Guid id, UpdateDeveloperRequest request, CancellationToken cancellationToken = default);

    Task DeleteDeveloperAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeveloperDto?> AddMappingByEmailAsync(
        Guid developerId,
        AddDeveloperMappingByEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<SyncDevelopersResult?> SyncFromClickUpAsync(CancellationToken cancellationToken = default);
}

public sealed class DeveloperApiClient : IDeveloperApiClient
{
    private readonly HttpClient _httpClient;

    public DeveloperApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<DeveloperDto>> GetDevelopersAsync(CancellationToken cancellationToken = default)
    {
        var developers = await _httpClient.GetFromJsonAsync<List<DeveloperDto>>("api/developers", cancellationToken);
        return developers ?? [];
    }

    public async Task<DeveloperDto?> CreateDeveloperAsync(CreateDeveloperRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/developers", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to create developer: {error}");
        }

        return await response.Content.ReadFromJsonAsync<DeveloperDto>(cancellationToken);
    }

    public async Task<DeveloperDto?> UpdateDeveloperAsync(Guid id, UpdateDeveloperRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/developers/{id}", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to update developer: {error}");
        }

        return await response.Content.ReadFromJsonAsync<DeveloperDto>(cancellationToken);
    }

    public async Task DeleteDeveloperAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/developers/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<DeveloperDto?> AddMappingByEmailAsync(
        Guid developerId,
        AddDeveloperMappingByEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/developers/{developerId}/mappings/by-email",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(ParseApiError(error, "Failed to map developer"));
        }

        return await response.Content.ReadFromJsonAsync<DeveloperDto>(cancellationToken);
    }

    public async Task<SyncDevelopersResult?> SyncFromClickUpAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/developers/sync", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to sync developers: {error}");
        }

        return await response.Content.ReadFromJsonAsync<SyncDevelopersResult>(cancellationToken);
    }

    private static string ParseApiError(string rawError, string fallback)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(rawError);
            if (document.RootElement.TryGetProperty("error", out var errorProperty))
            {
                return errorProperty.GetString() ?? fallback;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Use raw body below.
        }

        return string.IsNullOrWhiteSpace(rawError) ? fallback : rawError;
    }
}
