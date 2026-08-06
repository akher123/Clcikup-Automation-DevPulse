using DevPulse.Shared.Contracts.Reports;

namespace DevPulse.Client.Analytics;

public sealed record NamedCount(string Name, int Count);

public sealed record DeveloperKpi(
    Guid DeveloperId,
    string DeveloperName,
    string? Email,
    int TotalTasks,
    int CompletedCount,
    int InProgressCount,
    int OverdueCount,
    int OnTimeCompletedCount,
    double CompletionRate,
    double? OnTimeRate,
    double? AverageCycleDays,
    int WorkspaceCount,
    int ProjectCount);

public sealed record TeamKpis(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalTasks,
    int CompletedCount,
    int InProgressCount,
    int OverdueCount,
    int OnTimeCompletedCount,
    double CompletionRate,
    double? OnTimeRate,
    double? AverageCycleDays,
    int ActiveDevelopers,
    int WorkspaceCount,
    IReadOnlyList<DeveloperKpi> Developers,
    IReadOnlyList<NamedCount> ByWorkspace,
    IReadOnlyList<NamedCount> ByProject);

public static class KpiAnalytics
{
    public static TeamKpis FromReport(DeveloperReportResponse report)
    {
        var activeDevelopers = report.Developers
            .Where(d => d.TotalTasks > 0)
            .Select(ToDeveloperKpi)
            .OrderByDescending(d => d.CompletedCount)
            .ThenByDescending(d => d.CompletionRate)
            .ToList();

        var totalTasks = activeDevelopers.Sum(d => d.TotalTasks);
        var completed = report.TotalTasksCompleted;
        var overdue = activeDevelopers.Sum(d => d.OverdueCount);
        var onTime = activeDevelopers.Sum(d => d.OnTimeCompletedCount);

        var cycleSamples = activeDevelopers
            .Where(d => d.AverageCycleDays.HasValue && d.CompletedCount > 0)
            .Select(d => (Days: d.AverageCycleDays!.Value, Weight: d.CompletedCount))
            .ToList();

        double? avgCycle = null;
        if (cycleSamples.Count > 0)
        {
            var weight = cycleSamples.Sum(s => s.Weight);
            if (weight > 0)
            {
                avgCycle = Math.Round(cycleSamples.Sum(s => s.Days * s.Weight) / weight, 1);
            }
        }

        var byWorkspace = report.Developers
            .SelectMany(d => d.ByWorkspace)
            .GroupBy(w => w.AccountName)
            .Select(g => new NamedCount(g.Key, g.Sum(x => x.TaskCount)))
            .OrderByDescending(x => x.Count)
            .ToList();

        var byProject = report.Developers
            .SelectMany(d => d.ByProject)
            .GroupBy(p => p.ProjectName)
            .Select(g => new NamedCount(g.Key, g.Sum(x => x.TaskCount)))
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();

        return new TeamKpis(
            report.FromDate,
            report.ToDate,
            totalTasks,
            completed,
            report.TotalInProgress,
            overdue,
            onTime,
            Rate(completed, totalTasks),
            DeliveryHealth(onTime, overdue),
            avgCycle,
            activeDevelopers.Count,
            report.WorkspaceCount,
            activeDevelopers,
            byWorkspace,
            byProject);
    }

    private static DeveloperKpi ToDeveloperKpi(DeveloperReportSummaryDto summary) =>
        new(
            summary.DeveloperId,
            summary.DeveloperName,
            summary.Email,
            summary.TotalTasks,
            summary.CompletedCount,
            summary.InProgressCount,
            summary.OverdueCount,
            summary.OnTimeCompletedCount,
            Rate(summary.CompletedCount, summary.TotalTasks),
            DeliveryHealth(summary.OnTimeCompletedCount, summary.OverdueCount),
            summary.AverageCompletionDays,
            summary.WorkspaceCount,
            summary.ProjectCount);

    /// <summary>
    /// Delivery health: on-time completions vs on-time + overdue pressure.
    /// </summary>
    private static double? DeliveryHealth(int onTimeCompleted, int overdueCount)
    {
        var denominator = onTimeCompleted + overdueCount;
        return denominator > 0 ? Rate(onTimeCompleted, denominator) : null;
    }

    public static double Rate(int numerator, int denominator) =>
        denominator <= 0 ? 0 : Math.Round(numerator * 100.0 / denominator, 1);

    public static string FormatPercent(double? value) =>
        value.HasValue ? $"{value.Value:0.#}%" : "—";

    public static string FormatDays(double? value) =>
        value.HasValue ? $"{value.Value:0.#}d" : "—";

    public static string FormatPeriod(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate == toDate)
        {
            return fromDate.ToString("MMM d, yyyy");
        }

        if (fromDate.Year == toDate.Year && fromDate.Month == toDate.Month && fromDate.Day == 1
            && toDate.Day == DateTime.DaysInMonth(toDate.Year, toDate.Month))
        {
            return fromDate.ToString("MMMM yyyy");
        }

        return $"{fromDate:MMM d, yyyy} – {toDate:MMM d, yyyy}";
    }

    public static string PerformanceBand(double? deliveryHealth, double completionRate) =>
        (deliveryHealth ?? completionRate, completionRate) switch
        {
            ( >= 85, >= 70) => "Excellent",
            ( >= 70, >= 55) => "Strong",
            ( >= 50, >= 40) => "Steady",
            _ => "Watch"
        };

    public static string PerformanceTone(string band) => band switch
    {
        "Excellent" => "tone-excellent",
        "Strong" => "tone-strong",
        "Steady" => "tone-steady",
        _ => "tone-watch"
    };
}
