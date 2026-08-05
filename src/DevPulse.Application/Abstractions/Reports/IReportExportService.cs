using DevPulse.Shared.Contracts.Reports;

namespace DevPulse.Application.Abstractions.Reports;

public interface IReportExportService
{
    byte[] ExportDeveloperReportToExcel(DeveloperReportResponse report);
}
