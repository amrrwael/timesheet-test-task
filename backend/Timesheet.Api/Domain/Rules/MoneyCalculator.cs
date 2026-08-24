namespace Timesheet.Api.Domain.Rules;

/// <summary>
/// Деньги — только decimal, результат округляется до копеек.
/// Правило задания: double и float для денег не используются;
/// часы (double) конвертируются явно перед умножением.
/// </summary>
public static class MoneyCalculator
{
    public static decimal Cost(double hours, decimal hourlyRate) =>
        Math.Round((decimal)hours * hourlyRate, 2, MidpointRounding.AwayFromZero);
}