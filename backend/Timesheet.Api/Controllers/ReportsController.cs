using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Timesheet.Api.Contracts;
using Timesheet.Api.Services;

namespace Timesheet.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reports;
    private readonly IValidator<ProjectReportFilter> _filterValidator;

    public ReportsController(ReportService reports, IValidator<ProjectReportFilter> filterValidator)
    {
        _reports = reports;
        _filterValidator = filterValidator;
    }

    [HttpGet("projects")]
    public async Task<ActionResult<ProjectReportDto>> ProjectReport(
        [FromQuery] ProjectReportFilter filter, CancellationToken ct)
    {
        await _filterValidator.ValidateAndThrowAsync(filter, ct);
        return Ok(await _reports.GetProjectReportAsync(filter, ct));
    }
}