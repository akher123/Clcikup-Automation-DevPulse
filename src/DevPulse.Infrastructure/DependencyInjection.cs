using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Application.Abstractions.Security;
using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Infrastructure.ClickUp;
using DevPulse.Infrastructure.Persistence;
using DevPulse.Infrastructure.Persistence.Repositories;
using DevPulse.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevPulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DevPulse")
            ?? "Data Source=devpulse.db";

        services.AddDbContext<DevPulseDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddDataProtection();
        services.AddScoped<ITokenProtector, DataProtectionTokenProtector>();
        services.AddScoped<IClickUpAccountRepository, ClickUpAccountRepository>();
        services.AddScoped<IDeveloperRepository, DeveloperRepository>();

        services.AddHttpClient<IClickUpApiClient, ClickUpApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.clickup.com/api/v2/");
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DevPulseDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await EnsureDeveloperTablesAsync(dbContext);
    }

    private static async Task EnsureDeveloperTablesAsync(DevPulseDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS Developers (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Email TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Developers_Email ON Developers (Email);
            CREATE TABLE IF NOT EXISTS DeveloperClickUpMappings (
                Id TEXT NOT NULL PRIMARY KEY,
                DeveloperId TEXT NOT NULL,
                ClickUpAccountId TEXT NOT NULL,
                ClickUpUserId INTEGER NOT NULL,
                FOREIGN KEY (DeveloperId) REFERENCES Developers (Id) ON DELETE CASCADE,
                FOREIGN KEY (ClickUpAccountId) REFERENCES ClickUpAccounts (Id) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_DeveloperClickUpMappings_DeveloperId_ClickUpAccountId
                ON DeveloperClickUpMappings (DeveloperId, ClickUpAccountId);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_DeveloperClickUpMappings_ClickUpAccountId_ClickUpUserId
                ON DeveloperClickUpMappings (ClickUpAccountId, ClickUpUserId);
            """);
    }
}
