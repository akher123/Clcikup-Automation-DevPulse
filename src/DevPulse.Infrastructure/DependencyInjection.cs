using DevPulse.Application.Abstractions.Auth;
using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Application.Abstractions.Security;
using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Infrastructure.ClickUp;
using DevPulse.Infrastructure.Identity;
using DevPulse.Infrastructure.Persistence;
using DevPulse.Infrastructure.Persistence.Repositories;
using DevPulse.Infrastructure.Security;
using DevPulse.Infrastructure.Services;
using DevPulse.Shared.Constants;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevPulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DevPulse");
        services.AddDbContext<DevPulseDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SeedAdminSettings>(configuration.GetSection(SeedAdminSettings.SectionName));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<DevPulseDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "DevPulse.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddDataProtection();
        services.AddScoped<ITokenProtector, DataProtectionTokenProtector>();
        services.AddScoped<IClickUpAccountRepository, ClickUpAccountRepository>();
        services.AddScoped<IDeveloperRepository, DeveloperRepository>();
        services.AddSingleton<JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        services.AddHttpClient<IClickUpApiClient, ClickUpApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.clickup.com/api/v2/");
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromDays(2);
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DevPulseDbContext>();
        await dbContext.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(serviceProvider);
    }
}
