using DevPulse.Domain.Entities;
using DevPulse.Infrastructure.Identity;
using DevPulse.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DevPulse.Application.Abstractions.Security;

namespace DevPulse.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var seedSettings = scope.ServiceProvider.GetRequiredService<IOptions<SeedAdminSettings>>().Value;
        var dbContext = scope.ServiceProvider.GetRequiredService<DevPulseDbContext>();
        var tokenProtector = scope.ServiceProvider.GetRequiredService<ITokenProtector>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() });
            }
        }

        await SeedAdminUserAsync(userManager, seedSettings, logger);
        await SeedDemoClickUpAccountsAsync(dbContext, tokenProtector, logger);
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        SeedAdminSettings seedSettings,
        ILogger logger)
    {
        var admin = await userManager.FindByEmailAsync(seedSettings.Email);
        if (admin is not null)
        {
            return;
        }

        admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = seedSettings.Email,
            Email = seedSettings.Email,
            DisplayName = seedSettings.DisplayName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, seedSettings.Password);
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to seed admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        logger.LogInformation("Seeded default admin user {Email}", seedSettings.Email);
    }

    private static async Task SeedDemoClickUpAccountsAsync(
        DevPulseDbContext dbContext,
        ITokenProtector tokenProtector,
        ILogger logger)
    {
        if (await dbContext.ClickUpAccounts.AnyAsync(a => a.WorkspaceId.StartsWith(DemoSeedData.WorkspaceIdPrefix)))
        {
            return;
        }

        var encryptedToken = tokenProtector.Protect(DemoSeedData.DemoAccessToken);
        var seededAt = DateTime.UtcNow;

        var internalAccount = new ClickUpAccount
        {
            Id = DemoSeedData.InternalAccountId,
            Name = $"{AppBranding.CompanyName} Internal",
            WorkspaceId = $"{DemoSeedData.WorkspaceIdPrefix}DV-INTERNAL",
            EncryptedAccessToken = encryptedToken,
            IsActive = true,
            CreatedAtUtc = seededAt,
            LastValidatedAtUtc = seededAt,
            LastValidationMessage = "Demo workspace seeded for developer work reporting."
        };

        var acmeAccount = new ClickUpAccount
        {
            Id = DemoSeedData.AcmeAccountId,
            Name = "Client — Acme Corp",
            WorkspaceId = $"{DemoSeedData.WorkspaceIdPrefix}DV-ACME",
            EncryptedAccessToken = encryptedToken,
            IsActive = true,
            CreatedAtUtc = seededAt,
            LastValidatedAtUtc = seededAt,
            LastValidationMessage = "Demo workspace seeded for developer work reporting."
        };

        dbContext.ClickUpAccounts.AddRange(internalAccount, acmeAccount);
        await dbContext.SaveChangesAsync();
        logger.LogInformation(
            "Seeded demo ClickUp workspaces for {Company}: {AccountCount} accounts",
            AppBranding.CompanyName,
            2);
    }
}
