using DevPulse.Domain.Entities;

namespace DevPulse.Application.Abstractions.Persistence;

public interface IDeveloperRepository
{
    Task<IReadOnlyList<Developer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all developers that have at least one ClickUp mapping, regardless of IsActive.
    /// </summary>
    Task<IReadOnlyList<Developer>> GetWithMappingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Developer>> GetByIdsWithMappingsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);

    Task<Developer?> GetByIdWithMappingsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Developer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Developer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(Developer developer, CancellationToken cancellationToken = default);

    Task UpdateAsync(Developer developer, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> MappingExistsAsync(Guid developerId, Guid accountId, CancellationToken cancellationToken = default);

    Task AddMappingAsync(DeveloperClickUpMapping mapping, CancellationToken cancellationToken = default);
}
