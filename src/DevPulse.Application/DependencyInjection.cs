using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Application.Services.ClickUp;
using Microsoft.Extensions.DependencyInjection;

namespace DevPulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClickUpAccountService, ClickUpAccountService>();
        return services;
    }
}
