namespace Timesheet.Api.Contracts;

public record EmployeeDto(string Id, string Name, string Department, IReadOnlyList<RateDto> Rates);

public record RateDto(decimal Value, DateOnly From);

public record ProjectDto(
    string Id,
    string Code,
    string Name,
    decimal Budget,
    DateOnly StartDate,
    DateOnly? EndDate);