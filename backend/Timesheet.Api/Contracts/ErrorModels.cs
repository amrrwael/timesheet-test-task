namespace Timesheet.Api.Contracts;

public record ErrorResponse(string Code, string Message, IReadOnlyList<FieldError>? Errors = null);

public record FieldError(string Field, string Message);