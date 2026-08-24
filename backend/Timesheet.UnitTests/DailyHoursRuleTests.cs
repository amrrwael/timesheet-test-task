using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Exceptions;
using Timesheet.Api.Domain.Rules;

namespace Timesheet.UnitTests;

public class DailyHoursRuleTests
{
    [Fact]
    public void TwentySixHoursPerDay_Throws()
    {
        var ex = Assert.Throws<ConflictException>(
            () => DailyHoursRule.EnsureDayTotalAllowed(existingDayTotal: 20, additionalHours: 6));

        Assert.Equal(ErrorCodes.DailyLimitExceeded, ex.Code);
    }

    [Fact]
    public void ExactlyTwentyFourHoursPerDay_IsAllowed()
    {
        var ex = Record.Exception(
            () => DailyHoursRule.EnsureDayTotalAllowed(existingDayTotal: 16, additionalHours: 8));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(12, false)]   // 12 часов — ещё не переработка
    [InlineData(11.5, false)]
    [InlineData(12.5, true)]  // больше 12 — переработка
    [InlineData(24, true)]
    public void OvertimeFlag_FollowsTwelveHourThreshold(double dayTotal, bool expected)
    {
        Assert.Equal(expected, DailyHoursRule.IsOvertime(dayTotal));
    }
}