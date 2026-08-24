using Timesheet.Api.Domain.Rules;

namespace Timesheet.UnitTests;

public class HoursRuleTests
{
    [Theory]
    [InlineData(0)]     // ноль запрещён
    [InlineData(-4)]    // отрицательные запрещены
    [InlineData(3.7)]   // не кратны 0,5
    [InlineData(24.5)]  // больше 24
    [InlineData(25)]
    public void InvalidHours_Rejected(double hours)
    {
        Assert.False(HoursRule.IsValid(hours));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(8)]
    [InlineData(7.5)]
    [InlineData(23.5)]
    [InlineData(24)]
    public void ValidHours_Accepted(double hours)
    {
        Assert.True(HoursRule.IsValid(hours));
    }
}