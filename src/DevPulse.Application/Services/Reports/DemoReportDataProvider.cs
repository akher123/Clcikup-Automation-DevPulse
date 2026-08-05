using DevPulse.Domain.Entities;
using DevPulse.Shared.Constants;
using DevPulse.Shared.Contracts.Reports;

namespace DevPulse.Application.Services.Reports;

/// <summary>
/// Provides sample completed tasks for seeded demo workspaces so reports work without live ClickUp API calls.
/// </summary>
public static class DemoReportDataProvider
{
    private sealed record DemoTaskTemplate(
        Guid DeveloperId,
        Guid AccountId,
        string TaskId,
        string TaskName,
        string ListName,
        int CreatedDay,
        int CompletedDay,
        double CompletionDays);

    private static readonly DemoTaskTemplate[] TaskTemplates =
    [
        new(SarahChenId(), DemoSeedData.InternalAccountId, "dv-1001", "Implement SSO login flow", "Platform", 2, 5, 3.0),
        new(SarahChenId(), DemoSeedData.InternalAccountId, "dv-1002", "Add role-based access checks", "Platform", 6, 9, 3.0),
        new(SarahChenId(), DemoSeedData.AcmeAccountId, "dv-1003", "Build invoice export endpoint", "Acme Billing", 4, 8, 4.0),
        new(SarahChenId(), DemoSeedData.AcmeAccountId, "dv-1004", "Fix timezone handling in reports", "Acme Billing", 12, 14, 2.0),

        new(JamesOkonkwoId(), DemoSeedData.InternalAccountId, "dv-2001", "Optimize dashboard query performance", "Platform", 1, 4, 3.0),
        new(JamesOkonkwoId(), DemoSeedData.InternalAccountId, "dv-2002", "Set up CI pipeline for API", "DevOps", 7, 10, 3.0),
        new(JamesOkonkwoId(), DemoSeedData.AcmeAccountId, "dv-2003", "Integrate payment webhook handler", "Acme Payments", 3, 7, 4.0),

        new(PriyaSharmaId(), DemoSeedData.InternalAccountId, "dv-3001", "Redesign developer report filters", "UX", 2, 6, 4.0),
        new(PriyaSharmaId(), DemoSeedData.InternalAccountId, "dv-3002", "Add empty-state messaging", "UX", 8, 11, 3.0),
        new(PriyaSharmaId(), DemoSeedData.AcmeAccountId, "dv-3003", "Ship customer onboarding checklist", "Acme Delivery", 5, 9, 4.0),
        new(PriyaSharmaId(), DemoSeedData.AcmeAccountId, "dv-3004", "Resolve mobile layout regressions", "Acme Delivery", 13, 15, 2.0),

        new(MarcusWebbId(), DemoSeedData.InternalAccountId, "dv-4001", "Write integration tests for auth", "QA", 3, 8, 5.0),
        new(MarcusWebbId(), DemoSeedData.InternalAccountId, "dv-4002", "Automate regression suite in CI", "QA", 9, 13, 4.0),
        new(MarcusWebbId(), DemoSeedData.AcmeAccountId, "dv-4003", "Validate API contract changes", "Acme QA", 6, 10, 4.0),
    ];

    public static IReadOnlyList<DeveloperReportTaskDto> GetTasksForDateRange(
        DateOnly fromDate,
        DateOnly toDate,
        ClickUpAccount account,
        Developer developer)
    {
        if (!DemoSeedData.IsDemoWorkspace(account.WorkspaceId) || fromDate > toDate)
        {
            return [];
        }

        var fromMs = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var toExclusiveMs = new DateTimeOffset(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

        var tasks = new List<DeveloperReportTaskDto>();
        var monthCursor = new DateOnly(fromDate.Year, fromDate.Month, 1);
        var endMonth = new DateOnly(toDate.Year, toDate.Month, 1);

        while (monthCursor <= endMonth)
        {
            tasks.AddRange(GetTasksForMonth(monthCursor, account, developer));
            monthCursor = monthCursor.AddMonths(1);
        }

        return tasks
            .Where(t => t.DateDone.HasValue && t.DateDone.Value >= fromMs && t.DateDone.Value < toExclusiveMs)
            .ToList();
    }

    public static IReadOnlyList<DeveloperReportTaskDto> GetTasksForMonth(
        DateOnly month,
        ClickUpAccount account,
        Developer developer)
    {
        if (!DemoSeedData.IsDemoWorkspace(account.WorkspaceId))
        {
            return [];
        }

        return TaskTemplates
            .Where(t => t.DeveloperId == developer.Id && t.AccountId == account.Id)
            .Select(t => ToTaskDto(month, account, developer, t))
            .ToList();
    }

    private static DeveloperReportTaskDto ToTaskDto(
        DateOnly month,
        ClickUpAccount account,
        Developer developer,
        DemoTaskTemplate template)
    {
        var created = ToUnixMs(month, template.CreatedDay);
        var completed = ToUnixMs(month, template.CompletedDay);

        return new DeveloperReportTaskDto(
            developer.Id,
            developer.Name,
            account.Id,
            account.Name,
            template.TaskId,
            template.TaskName,
            "complete",
            template.ListName,
            null,
            created,
            completed,
            template.CompletionDays);
    }

    private static long ToUnixMs(DateOnly month, int day)
    {
        var clampedDay = Math.Clamp(day, 1, DateTime.DaysInMonth(month.Year, month.Month));
        var date = new DateTimeOffset(month.Year, month.Month, clampedDay, 12, 0, 0, TimeSpan.Zero);
        return date.ToUnixTimeMilliseconds();
    }

    private static Guid SarahChenId() => DemoSeedData.SarahChenId;

    private static Guid JamesOkonkwoId() => DemoSeedData.JamesOkonkwoId;

    private static Guid PriyaSharmaId() => DemoSeedData.PriyaSharmaId;

    private static Guid MarcusWebbId() => DemoSeedData.MarcusWebbId;
}
