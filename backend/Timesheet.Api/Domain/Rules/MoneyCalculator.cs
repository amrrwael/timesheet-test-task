namespace Timesheet.Api.Domain.Rules;

/// <summary>
/// Деньги — только decimal, результат округляется до копеек.
/// Правило округления — «к ближайшему чётному» (banker's), сознательно совпадает
/// с оператором $round в агрегациях MongoDB: одинаковая семантика в C# и в БД.
/// </summary>
public static class MoneyCalculator
{
    public static decimal Cost(double hours, decimal hourlyRate) =>
        Math.Round((decimal)hours * hourlyRate, 2, MidpointRounding.ToEven);
}