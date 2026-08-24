namespace Timesheet.Api.Contracts;

public interface ITimeEntryWriteRequest
{
    string EmployeeId { get; }
    string ProjectId { get; }
    string Date { get; }
    double Hours { get; }
    string? Comment { get; }
}

/// <summary>Создание записи (метод PUT по спецификации).</summary>
public record CreateTimeEntryRequest(
    string EmployeeId,
    string ProjectId,
    string Date,
    double Hours,
    string? Comment) : ITimeEntryWriteRequest;

/// <summary>Изменение записи; Version — версия, которую клиент видел при открытии.</summary>
public record UpdateTimeEntryRequest(
    string EmployeeId,
    string ProjectId,
    string Date,
    double Hours,
    string? Comment,
    int Version) : ITimeEntryWriteRequest;

public record TimeEntryDto(
    string Id,
    string EmployeeId,
    string EmployeeName,
    string ProjectId,
    string ProjectCode,
    string ProjectName,
    string Date,
    double Hours,
    decimal Rate,
    decimal Amount,
    string? Comment,
    int Version,
    bool Overtime);

public record TimeEntriesFilter(
    int Year,
    int Month,
    string? EmployeeId = null,
    string? ProjectId = null,
    int Page = 1,
    int PageSize = 20);

public record TimeEntriesPageDto(
    IReadOnlyList<TimeEntryDto> Items,
    int Page,
    int PageSize,
    long TotalCount,
    double TotalHours,
    decimal TotalAmount);