namespace DevPulse.Shared.Contracts.Developers;

public record DeveloperDto(
    Guid Id,
    string Name,
    string? Email,
    bool IsActive,
    DateTime CreatedAtUtc,
    IReadOnlyList<DeveloperClickUpMappingDto> Mappings);

public record DeveloperClickUpMappingDto(
    Guid Id,
    Guid ClickUpAccountId,
    string AccountName,
    int ClickUpUserId);

public record CreateDeveloperRequest(
    string Name,
    string? Email);

public record UpdateDeveloperRequest(
    string Name,
    string? Email,
    bool IsActive);

public record AddDeveloperMappingRequest(
    Guid ClickUpAccountId,
    int ClickUpUserId);

public record AddDeveloperMappingByEmailRequest(
    string WorkspaceId,
    string? Email = null);

public record SyncDevelopersResult(
    int DevelopersCreated,
    int DevelopersUpdated,
    int MappingsAdded);

public record SyncFromClickUpRequest(Guid? ClickUpAccountId = null);
