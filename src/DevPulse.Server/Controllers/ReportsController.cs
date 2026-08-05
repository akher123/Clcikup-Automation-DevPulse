using DevPulse.Application.Abstractions.Reports;
using DevPulse.Shared.Contracts.Reports;
using Microsoft.AspNetCore.Mvc;

namespace DevPulse.Server.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost("developer-tasks")]
    [ProducesResponseType(typeof(DeveloperReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateDeveloperReport(
        [FromBody] DeveloperReportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DeveloperIds is null || request.DeveloperIds.Count == 0)
        {
            return BadRequest(new { error = "Select at least one developer." });
        }

        try
        {
            var result = await _reportService.GenerateDeveloperReportAsync(request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error, errors = result.Errors });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
