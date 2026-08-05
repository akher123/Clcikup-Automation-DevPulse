using DevPulse.Application.Abstractions.Developers;
using DevPulse.Application.Abstractions.Reports;
using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Application.Abstractions.Persistence;
using DevPulse.Application.Services.ClickUp;
using DevPulse.Application.Services.Developers;
using DevPulse.Application.Services.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace DevPulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClickUpAccountService, ClickUpAccountService>();
        services.AddScoped<IDeveloperService, DeveloperService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportExportService, ReportExcelExportService>();
        return services;
    }
}
