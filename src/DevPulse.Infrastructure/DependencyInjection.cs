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
    }
}
