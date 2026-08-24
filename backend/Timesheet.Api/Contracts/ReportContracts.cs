namespace Timesheet.Api.Contracts;

public record ProjectReportFilter(int Year, int Month);

public record ProjectReportRowDto(
    string ProjectId,
    string Code,
    string Name,
    decimal Budget,
    double Hours,
    decimal Amount,
    decimal? Percent,
    bool Overspent,
    bool AtRisk);

public record ProjectReportDto(
    IReadOnlyList<ProjectReportRowDto> Projects,
    double TotalHours,
    decimal TotalAmount);