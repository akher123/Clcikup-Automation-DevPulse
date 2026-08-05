using DevPulse.Application.Abstractions.Developers;
using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Application.Abstractions.Security;
using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Domain.Entities;
using DevPulse.Shared.Common;
using DevPulse.Shared.Constants;
using DevPulse.Shared.Contracts.Developers;
using Microsoft.Extensions.Logging;

namespace DevPulse.Application.Services.Developers;

public sealed class DeveloperService : IDeveloperService
{
    private readonly IDeveloperRepository _developerRepository;
    private readonly IClickUpAccountRepository _accountRepository;
    private readonly IClickUpAccountService _accountService;
    private readonly IClickUpApiClient _apiClient;
    private readonly ITokenProtector _tokenProtector;
    private readonly ILogger<DeveloperService> _logger;

    public DeveloperService(
        IDeveloperRepository developerRepository,
        IClickUpAccountRepository accountRepository,
        IClickUpAccountService accountService,
        IClickUpApiClient apiClient,
        ITokenProtector tokenProtector,
        ILogger<DeveloperService> logger)
    {
        _developerRepository = developerRepository;
        _accountRepository = accountRepository;
        _accountService = accountService;
        _apiClient = apiClient;
        _tokenProtector = tokenProtector;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeveloperDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var developers = await _developerRepository.GetAllAsync(cancellationToken);
        return developers.Select(MapToDto).ToList();
    }

    public async Task<Result<DeveloperDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var developer = await _developerRepository.GetByIdWithMappingsAsync(id, cancellationToken);
        return developer is null
            ? Result<DeveloperDto>.Failure("Developer was not found.")
            : Result<DeveloperDto>.Success(MapToDto(developer));
    }

    public async Task<Result<DeveloperDto>> CreateAsync(CreateDeveloperRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateDeveloperRequest(request.Name, request.Email);
        if (validation.IsFailure)
        {
            return Result<DeveloperDto>.Failure(validation.Error!);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail is not null)
        {
            var existing = await _developerRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (existing is not null)
            {
                return Result<DeveloperDto>.Failure("A developer with this email already exists.");
            }
        }

        var developer = new Developer
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail
        };

        await _developerRepository.AddAsync(developer, cancellationToken);
        _logger.LogInformation("Created developer {DeveloperName}", developer.Name);

        var created = await _developerRepository.GetByIdWithMappingsAsync(developer.Id, cancellationToken);
        return Result<DeveloperDto>.Success(MapToDto(created!));
    }

    public async Task<Result<DeveloperDto>> UpdateAsync(Guid id, UpdateDeveloperRequest request, CancellationToken cancellationToken = default)
    {
        var developer = await _developerRepository.GetByIdAsync(id, cancellationToken);
        if (developer is null)
        {
            return Result<DeveloperDto>.Failure("Developer was not found.");
        }

        var validation = ValidateDeveloperRequest(request.Name, request.Email);
        if (validation.IsFailure)
        {
            return Result<DeveloperDto>.Failure(validation.Error!);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail is not null)
        {
            var existing = await _developerRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (existing is not null && existing.Id != id)
            {
                return Result<DeveloperDto>.Failure("Another developer with this email already exists.");
            }
        }

        developer.Name = request.Name.Trim();
        developer.Email = normalizedEmail;
        developer.IsActive = request.IsActive;

        await _developerRepository.UpdateAsync(developer, cancellationToken);

        var updated = await _developerRepository.GetByIdWithMappingsAsync(id, cancellationToken);
        return Result<DeveloperDto>.Success(MapToDto(updated!));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var developer = await _developerRepository.GetByIdAsync(id, cancellationToken);
        if (developer is null)
        {
            return Result.Failure("Developer was not found.");
        }

        await _developerRepository.DeleteAsync(id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<DeveloperDto>> AddMappingAsync(Guid developerId, AddDeveloperMappingRequest request, CancellationToken cancellationToken = default)
    {
        var developer = await _developerRepository.GetByIdWithMappingsAsync(developerId, cancellationToken);
        if (developer is null)
        {
            return Result<DeveloperDto>.Failure("Developer was not found.");
        }

        var account = await _accountRepository.GetByIdAsync(request.ClickUpAccountId, cancellationToken);
        if (account is null)
        {
            return Result<DeveloperDto>.Failure("ClickUp account was not found.");
        }

        if (await _developerRepository.MappingExistsAsync(developerId, request.ClickUpAccountId, cancellationToken))
        {
            return Result<DeveloperDto>.Failure("This developer is already mapped to the selected workspace.");
        }

        var mapping = new DeveloperClickUpMapping
        {
            DeveloperId = developerId,
            ClickUpAccountId = request.ClickUpAccountId,
            ClickUpUserId = request.ClickUpUserId
        };

        await _developerRepository.AddMappingAsync(mapping, cancellationToken);

        var updated = await _developerRepository.GetByIdWithMappingsAsync(developerId, cancellationToken);
        return Result<DeveloperDto>.Success(MapToDto(updated!));
    }

    public async Task<Result<DeveloperDto>> AddMappingByEmailAsync(
        Guid developerId,
        AddDeveloperMappingByEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var developer = await _developerRepository.GetByIdWithMappingsAsync(developerId, cancellationToken);
        if (developer is null)
        {
            return Result<DeveloperDto>.Failure("Developer was not found.");
        }

        var email = NormalizeEmail(request.Email) ?? NormalizeEmail(developer.Email);
        if (email is null)
        {
            return Result<DeveloperDto>.Failure("Email is required to resolve the ClickUp user.");
        }

        var lookup = await _accountService.GetMemberByEmailAsync(request.WorkspaceId, email, cancellationToken);
        if (lookup.IsFailure)
        {
            return Result<DeveloperDto>.Failure(lookup.Error!);
        }

        var member = lookup.Value!;
        if (await _developerRepository.MappingExistsAsync(developerId, member.AccountId, cancellationToken))
        {
            return Result<DeveloperDto>.Failure("This developer is already mapped to the selected workspace.");
        }

        await _developerRepository.AddMappingAsync(new DeveloperClickUpMapping
        {
            DeveloperId = developerId,
            ClickUpAccountId = member.AccountId,
            ClickUpUserId = member.ClickUpUserId
        }, cancellationToken);

        _logger.LogInformation(
            "Mapped developer {DeveloperId} to ClickUp user {ClickUpUserId} in workspace {WorkspaceId} via email lookup",
            developerId,
            member.ClickUpUserId,
            member.WorkspaceId);

        var updated = await _developerRepository.GetByIdWithMappingsAsync(developerId, cancellationToken);
        return Result<DeveloperDto>.Success(MapToDto(updated!));
    }

    public async Task<Result<SyncDevelopersResult>> SyncFromClickUpAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetActiveAsync(cancellationToken);
        if (accounts.Count == 0)
        {
            return Result<SyncDevelopersResult>.Failure("No active ClickUp accounts configured.");
        }

        var created = 0;
        var updated = 0;
        var mappingsAdded = 0;

        foreach (var account in accounts)
        {
            if (DemoSeedData.IsDemoWorkspace(account.WorkspaceId))
            {
                continue;
            }

            var token = _tokenProtector.Unprotect(account.EncryptedAccessToken);
            var members = await _apiClient.GetWorkspaceMembersAsync(token, account.WorkspaceId, cancellationToken);

            foreach (var member in members)
            {
                var normalizedEmail = NormalizeEmail(member.Email);
                if (normalizedEmail is null)
                {
                    continue;
                }

                var developer = await _developerRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
                if (developer is null)
                {
                    developer = new Developer
                    {
                        Name = member.Username.Trim(),
                        Email = normalizedEmail
                    };
                    await _developerRepository.AddAsync(developer, cancellationToken);
                    created++;
                }
                else if (!string.Equals(developer.Name, member.Username.Trim(), StringComparison.Ordinal))
                {
                    developer.Name = member.Username.Trim();
                    await _developerRepository.UpdateAsync(developer, cancellationToken);
                    updated++;
                }

                if (!await _developerRepository.MappingExistsAsync(developer.Id, account.Id, cancellationToken))
                {
                    await _developerRepository.AddMappingAsync(new DeveloperClickUpMapping
                    {
                        DeveloperId = developer.Id,
                        ClickUpAccountId = account.Id,
                        ClickUpUserId = member.ClickUpUserId
                    }, cancellationToken);
                    mappingsAdded++;
                }
            }
        }

        _logger.LogInformation(
            "Synced developers from ClickUp: {Created} created, {Updated} updated, {MappingsAdded} mappings added",
            created, updated, mappingsAdded);

        return Result<SyncDevelopersResult>.Success(new SyncDevelopersResult(created, updated, mappingsAdded));
    }

    private static Result ValidateDeveloperRequest(string name, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure("Developer name is required.");
        }

        if (email is not null && string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure("Email cannot be empty when provided.");
        }

        return Result.Success();
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static DeveloperDto MapToDto(Developer developer) =>
        new(
            developer.Id,
            developer.Name,
            developer.Email,
            developer.IsActive,
            developer.CreatedAtUtc,
            developer.ClickUpMappings
                .Select(m => new DeveloperClickUpMappingDto(
                    m.Id,
                    m.ClickUpAccountId,
                    m.ClickUpAccount?.Name ?? "Unknown",
                    m.ClickUpUserId))
                .OrderBy(m => m.AccountName)
                .ToList());
}
