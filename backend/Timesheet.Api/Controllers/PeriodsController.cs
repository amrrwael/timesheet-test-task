using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Timesheet.Api.Contracts;
using Timesheet.Api.Services;

namespace Timesheet.Api.Controllers;

[ApiController]
[Route("api/periods")]
public class PeriodsController : ControllerBase
{
    private readonly PeriodService _periods;
    private readonly IValidator<PeriodRequest> _validator;

    public PeriodsController(PeriodService periods, IValidator<PeriodRequest> validator)
    {
        _periods = periods;
        _validator = validator;
    }

    /// <summary>Список закрытых месяцев — интерфейсу нужно показывать замки.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PeriodDto>>> GetAll(CancellationToken ct) =>
        Ok(await _periods.GetAllAsync(ct));

    [HttpPost("close")]
    public async Task<ActionResult<PeriodDto>> Close(
        [FromBody] PeriodRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _periods.CloseAsync(request, ct));
    }

    [HttpPost("open")]
    public async Task<IActionResult> Open(
        [FromBody] PeriodRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        await _periods.OpenAsync(request, ct);
        return NoContent();
    }
}