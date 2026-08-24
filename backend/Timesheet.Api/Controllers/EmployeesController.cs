using Microsoft.AspNetCore.Mvc;
using Timesheet.Api.Contracts;
using Timesheet.Api.Services;

namespace Timesheet.Api.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly ReferenceService _reference;

    public EmployeesController(ReferenceService reference)
    {
        _reference = reference;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetAll(CancellationToken ct)
    {
        var employees = await _reference.GetEmployeesAsync(ct);
        return Ok(employees.Select(Mappings.ToDto).ToList());
    }
}