using Timesheet.Api.Domain.Rules;

namespace Timesheet.UnitTests;

public class MoneyCalculatorTests
{
    [Fact]
    public void WholeHours_WholeRate()
    {
        Assert.Equal(4000m, MoneyCalculator.Cost(hours: 8, hourlyRate: 500m));
    }

    [Fact]
    public void HalfHours()
    {
        Assert.Equal(5250m, MoneyCalculator.Cost(hours: 7.5, hourlyRate: 700m));
    }

    [Fact]
    public void TieCase_RoundsToEven_MatchingMongoRound()
    {
        // 7.5 × 600.55 = 4504.125 — третий знак ровно 5.
        // Банковское правило (как $round в MongoDB): к чётной копейке → 4504.12.
        Assert.Equal(4504.12m, MoneyCalculator.Cost(hours: 7.5, hourlyRate: 600.55m));
    }

    [Fact]
    public void KopecksArePreserved()
    {
        Assert.Equal(3333.30m, MoneyCalculator.Cost(hours: 10, hourlyRate: 333.33m));
    }
}