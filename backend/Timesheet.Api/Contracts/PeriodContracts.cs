namespace Timesheet.Api.Contracts;

public record PeriodRequest(int Year, int Month);

public record PeriodDto(int Year, int Month, DateTime ClosedAt);

/// <summary>Добавление/замена ставки сотрудника с даты.</summary>
public record AddRateRequest(string From, decimal Value);