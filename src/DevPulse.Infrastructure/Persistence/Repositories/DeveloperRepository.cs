using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevPulse.Infrastructure.Persistence.Repositories;

public sealed class DeveloperRepository : IDeveloperRepository
{
    private readonly DevPulseDbContext _dbContext;

    public DeveloperRepository(DevPulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Developer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await QueryWithMappings()
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Developer>> GetActiveWithMappingsAsync(CancellationToken cancellationToken = default) =>
        await QueryWithMappings()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Developer>> GetByIdsWithMappingsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) =>
        await QueryWithMappings()
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Developer?> GetByIdWithMappingsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await QueryWithMappings()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Developer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.Developers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Developer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _dbContext.Developers
            .FirstOrDefaultAsync(x => x.Email != null && x.Email == email, cancellationToken);

    public async Task AddAsync(Developer developer, CancellationToken cancellationToken = default)
    {
        _dbContext.Developers.Add(developer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Developer developer, CancellationToken cancellationToken = default)
    {
        _dbContext.Developers.Update(developer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var developer = await _dbContext.Developers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (developer is null)
        {
            return;
        }

        _dbContext.Developers.Remove(developer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> MappingExistsAsync(Guid developerId, Guid accountId, CancellationToken cancellationToken = default) =>
        await _dbContext.DeveloperClickUpMappings
            .AnyAsync(x => x.DeveloperId == developerId && x.ClickUpAccountId == accountId, cancellationToken);

    public async Task AddMappingAsync(DeveloperClickUpMapping mapping, CancellationToken cancellationToken = default)
    {
        _dbContext.DeveloperClickUpMappings.Add(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Developer> QueryWithMappings() =>
        _dbContext.Developers
            .Include(x => x.ClickUpMappings)
            .ThenInclude(x => x.ClickUpAccount);
}
