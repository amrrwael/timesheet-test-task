using Timesheet.Api.Domain.Entities;

namespace Timesheet.Api.Domain.Rules;

/// <summary>
/// Ставка, действовавшая на дату записи: последняя по дате начала
/// среди ставок с From &lt;= даты записи. Ставка действует с даты From
/// до начала следующей.
/// Возвращает null, если на эту дату ставки ещё нет.
/// </summary>
public static class RateResolver
{
    public static decimal? Resolve(IReadOnlyList<Rate> rates, DateTime dateUtcMidnight)
    {
        Rate? effective = null;

        foreach (var rate in rates)
        {
            if (rate.From > dateUtcMidnight)
                continue;

            if (effective is null || rate.From > effective.From)
                effective = rate;
        }

        return effective?.Value;
    }
}