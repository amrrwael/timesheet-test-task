using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Timesheet.Api.Contracts;
using Timesheet.Api.Services;

namespace Timesheet.Api.Controllers;

[ApiController]
[Route("api/time-entries")]
public class TimeEntriesController : ControllerBase
{
    private readonly TimeEntryService _service;
    private readonly IValidator<CreateTimeEntryRequest> _createValidator;
    private readonly IValidator<UpdateTimeEntryRequest> _updateValidator;

    public TimeEntriesController(
        TimeEntryService service,
        IValidator<CreateTimeEntryRequest> createValidator,
        IValidator<UpdateTimeEntryRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Создание записи. Метод PUT задан спецификацией.</summary>
    [HttpPut]
    public async Task<ActionResult<TimeEntryDto>> Create(
        [FromBody] CreateTimeEntryRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);
        var created = await _service.CreateAsync(request, GetUserName(), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("{id}")]
    public async Task<ActionResult<TimeEntryDto>> Update(
        string id, [FromBody] UpdateTimeEntryRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.UpdateAsync(id, request, GetUserName(), ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Авторизации в задании нет; автора берём из заголовка, если он передан.</summary>
    private string GetUserName() =>
        Request.Headers["X-User-Name"].FirstOrDefault() ?? "неизвестный пользователь";
}