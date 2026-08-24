using Microsoft.AspNetCore.Mvc;
using Timesheet.Api.Contracts;
using Timesheet.Api.Services;

namespace Timesheet.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ReferenceService _reference;

    public ProjectsController(ReferenceService reference)
    {
        _reference = reference;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll(CancellationToken ct)
    {
        var projects = await _reference.GetProjectsAsync(ct);
        return Ok(projects.Select(Mappings.ToDto).ToList());
    }
}