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
    private readonly TimeEntryQueryService _queryService;
    private readonly IValidator<TimeEntriesFilter> _filterValidator;

    public TimeEntriesController(
        TimeEntryService service,
        IValidator<CreateTimeEntryRequest> createValidator,
        IValidator<UpdateTimeEntryRequest> updateValidator,
        TimeEntryQueryService queryService,
        IValidator<TimeEntriesFilter> filterValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _queryService = queryService;
        _filterValidator = filterValidator;
    }

    [HttpGet]
    public async Task<ActionResult<TimeEntriesPageDto>> GetPage(
        [FromQuery] TimeEntriesFilter filter, CancellationToken ct)
    {
        await _filterValidator.ValidateAndThrowAsync(filter, ct);
        return Ok(await _queryService.GetPageAsync(filter, ct));
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