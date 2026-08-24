using Timesheet.Api.Domain.Entities;
namespace Timesheet.Api.Contracts;

public static class Mappings
{
    public static EmployeeDto ToDto(this Employee e) => new(
        e.Id,
        e.Name,
        e.Department,
        e.Rates
            .OrderBy(r => r.From)
            .Select(r => new RateDto(r.Value, DateOnly.FromDateTime(r.From)))
            .ToList());

    public static ProjectDto ToDto(this Project p) => new(
        p.Id,
        p.Code,
        p.Name,
        p.Budget,
        DateOnly.FromDateTime(p.StartDate),
        p.EndDate.HasValue ? DateOnly.FromDateTime(p.EndDate.Value) : null);
}