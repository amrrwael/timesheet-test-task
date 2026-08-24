using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Timesheet.Api.Contracts;
using Timesheet.Api.Services;

namespace Timesheet.Api.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly ReferenceService _reference;
    private readonly IValidator<AddRateRequest> _rateValidator;

    public EmployeesController(ReferenceService reference, IValidator<AddRateRequest> rateValidator)
    {
        _reference = reference;
        _rateValidator = rateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetAll(CancellationToken ct)
    {
        var employees = await _reference.GetEmployeesAsync(ct);
        return Ok(employees.Select(Mappings.ToDto).ToList());
    }

    /// <summary>Добавить ставку с даты (или заменить ставку с той же датой начала).</summary>
    [HttpPost("{id}/rates")]
    public async Task<ActionResult<EmployeeDto>> AddRate(
        string id, [FromBody] AddRateRequest request, CancellationToken ct)
    {
        await _rateValidator.ValidateAndThrowAsync(request, ct);

        var from = DateNormalizer.Normalize(request.From);
        var employee = await _reference.AddRateAsync(id, from, request.Value, ct);

        return Ok(Mappings.ToDto(employee));
    }
}