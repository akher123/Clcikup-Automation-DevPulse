using DevPulse.Shared.Common;
using DevPulse.Shared.Contracts.Developers;

namespace DevPulse.Application.Abstractions.Developers;

public interface IDeveloperService
{
    Task<IReadOnlyList<DeveloperDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<DeveloperDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<DeveloperDto>> CreateAsync(CreateDeveloperRequest request, CancellationToken cancellationToken = default);

    Task<Result<DeveloperDto>> UpdateAsync(Guid id, UpdateDeveloperRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<DeveloperDto>> AddMappingAsync(Guid developerId, AddDeveloperMappingRequest request, CancellationToken cancellationToken = default);

    Task<Result<DeveloperDto>> AddMappingByEmailAsync(
        Guid developerId,
        AddDeveloperMappingByEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SyncDevelopersResult>> SyncFromClickUpAsync(
        SyncFromClickUpRequest? request = null,
        CancellationToken cancellationToken = default);
}
