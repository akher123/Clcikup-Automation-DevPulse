using DevPulse.Application.Abstractions.Analytics;
using DevPulse.Application.Abstractions.Attendance;
using DevPulse.Application.Abstractions.Developers;
using DevPulse.Application.Abstractions.Holidays;
using DevPulse.Application.Abstractions.Leave;
using DevPulse.Application.Abstractions.Reports;
using DevPulse.Application.Abstractions.ClickUp;
using DevPulse.Application.Options;
using DevPulse.Application.Services.Analytics;
using DevPulse.Application.Services.Attendance;
using DevPulse.Application.Services.ClickUp;
using DevPulse.Application.Services.Developers;
using DevPulse.Application.Services.Holidays;
using DevPulse.Application.Services.Leave;
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
        services.AddScoped<IKpiSyncService, KpiSyncService>();
        services.AddScoped<ICachedAnalyticsService, CachedAnalyticsService>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddScoped<LeaveDayCalculator>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<AttendanceStatusCalculator>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        return services;
    }
}
