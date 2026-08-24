namespace Timesheet.Api.Domain.Rules;

/// <summary>Часы одной записи: положительные, кратные 0,5, не больше 24.</summary>
public static class HoursRule
{
    private const double Epsilon = 1e-9;

    public static bool IsValid(double hours) =>
        hours > 0 &&
        hours <= DailyHoursRule.MaxHoursPerDay &&
        Math.Abs(hours % 0.5) < Epsilon;
}