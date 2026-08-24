using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Exceptions;
using Timesheet.Api.Domain.Rules;

namespace Timesheet.UnitTests;

public class ClosedPeriodGuardTests
{
    [Fact]
    public void ClosedPeriod_ThrowsPeriodClosed()
    {
        var ex = Assert.Throws<ConflictException>(() => ClosedPeriodGuard.EnsureOpen(isClosed: true, 2026, 2));

        Assert.Equal(ErrorCodes.PeriodClosed, ex.Code);
        Assert.Contains("02.2026", ex.Message);
    }

    [Fact]
    public void OpenPeriod_Passes()
    {
        var ex = Record.Exception(() => ClosedPeriodGuard.EnsureOpen(isClosed: false, 2026, 2));
        Assert.Null(ex);
    }
}