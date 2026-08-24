using System.Globalization;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Exceptions;

namespace Timesheet.Api.Domain.Rules;

/// <summary>
/// Лимиты часов: суммарно за один календарный день по всем проектам —
/// не больше 24 ч (иначе отказ); день с итогом больше 12 ч — переработка.
/// </summary>
public static class DailyHoursRule
{
    public const double MaxHoursPerDay = 24;
    public const double OvertimeThreshold = 12;

    /// <param name="existingDayTotal">уже отработано сотрудником в эту дату по всем проектам</param>
    /// <param name="additionalHours">часы создаваемой/изменяемой записи</param>
    public static void EnsureDayTotalAllowed(double existingDayTotal, double additionalHours)
    {
        var total = existingDayTotal + additionalHours;

        if (total > MaxHoursPerDay)
            throw new ConflictException(
                ErrorCodes.DailyLimitExceeded,
                $"Суммарно за день у сотрудника получится {Format(total)} ч " +
                $"при лимите {Format(MaxHoursPerDay)} ч.");
    }

    public static bool IsOvertime(double dayTotal) => dayTotal > OvertimeThreshold;

    private static string Format(double value) =>
        value.ToString("0.#", CultureInfo.InvariantCulture);
}