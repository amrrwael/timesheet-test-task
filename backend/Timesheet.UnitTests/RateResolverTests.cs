using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Domain.Rules;

namespace Timesheet.UnitTests;

public class RateResolverTests
{
    private static DateTime Utc(int year, int month, int day) =>
        DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

    /// <summary>Ставки Иванова из приёмочных данных: 500 с января, 600 с марта.</summary>
    private static List<Rate> Rates() => new()
    {
        new() { From = Utc(2026, 1, 1), Value = 500m },
        new() { From = Utc(2026, 3, 1), Value = 600m }
    };

    [Theory]
    [InlineData("2026-01-01", 500)] // ровно дата начала первой ставки — уже действует
    [InlineData("2026-02-20", 500)]
    [InlineData("2026-02-28", 500)] // последний день действия старой ставки
    [InlineData("2026-03-01", 600)] // ровно дата начала новой ставки
    [InlineData("2026-03-05", 600)]
    public void Resolve_PicksRateEffectiveOnDate(string isoDate, decimal expected)
    {
        var date = DateNormalizerForTests.Normalize(isoDate);

        var rate = RateResolver.Resolve(Rates(), date);

        Assert.Equal(expected, rate);
    }

    [Fact]
    public void Resolve_BeforeAnyRate_ReturnsNull()
    {
        var rate = RateResolver.Resolve(Rates(), Utc(2025, 12, 31));

        Assert.Null(rate);
    }

    [Fact]
    public void Resolve_UnsortedHistoryStillFindsLatestApplicable()
    {
        var unsorted = new List<Rate>
        {
            new() { From = Utc(2026, 3, 1), Value = 600m },
            new() { From = Utc(2026, 1, 1), Value = 500m }
        };

        Assert.Equal(500m, RateResolver.Resolve(unsorted, Utc(2026, 2, 20)));
        Assert.Equal(600m, RateResolver.Resolve(unsorted, Utc(2026, 3, 5)));
    }

    private static class DateNormalizerForTests
    {
        public static DateTime Normalize(string isoDate) =>
            DateTime.SpecifyKind(DateTime.Parse(isoDate), DateTimeKind.Utc);
    }
}