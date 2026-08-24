using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Domain.Exceptions;
using Timesheet.Api.Domain.Rules;

namespace Timesheet.UnitTests;

public class ProjectPeriodRuleTests
{
    private static DateTime Utc(int year, int month, int day) =>
        DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

    /// <summary>П-001 из приёмочных данных: 01.01.2026 – 31.03.2026.</summary>
    private static Project BoundedProject() => new()
    {
        Id = "p1",
        Code = "П-001",
        Name = "Реконструкция цеха",
        Budget = 20000m,
        StartDate = Utc(2026, 1, 1),
        EndDate = Utc(2026, 3, 31)
    };

    /// <summary>П-002: бессрочный, стартует 01.03.2026.</summary>
    private static Project OpenEndedProject() => new()
    {
        Id = "p2",
        Code = "П-002",
        Name = "Инженерные сети",
        Budget = 5000m,
        StartDate = Utc(2026, 3, 1),
        EndDate = null
    };

    [Fact]
    public void EntryOnStartDate_IsAllowed()
    {
        var ex = Record.Exception(() => ProjectPeriodRule.EnsureDateAllowed(BoundedProject(), Utc(2026, 1, 1)));
        Assert.Null(ex);
    }

    [Fact]
    public void EntryOnEndDate_IsAllowed()
    {
        var ex = Record.Exception(() => ProjectPeriodRule.EnsureDateAllowed(BoundedProject(), Utc(2026, 3, 31)));
        Assert.Null(ex);
    }

    [Fact]
    public void EntryBeforeStart_ThrowsProjectDateRange()
    {
        var ex = Assert.Throws<BusinessRuleException>(
            () => ProjectPeriodRule.EnsureDateAllowed(BoundedProject(), Utc(2025, 12, 31)));

        Assert.Equal(ErrorCodes.ProjectDateRange, ex.Code);
    }

    [Fact]
    public void EntryAfterEnd_ThrowsProjectDateRange()
    {
        var ex = Assert.Throws<BusinessRuleException>(
            () => ProjectPeriodRule.EnsureDateAllowed(BoundedProject(), Utc(2026, 4, 1)));

        Assert.Equal(ErrorCodes.ProjectDateRange, ex.Code);
    }

    [Fact]
    public void OpenEndedProject_AnyDateAfterStart_IsAllowed()
    {
        var ex = Record.Exception(() => ProjectPeriodRule.EnsureDateAllowed(OpenEndedProject(), Utc(2027, 6, 15)));
        Assert.Null(ex);
    }

    [Fact]
    public void OpenEndedProject_DateBeforeStart_Throws()
    {
        // сценарий приёмки №4: запись на П-002 датой 20.02.2026
        var ex = Assert.Throws<BusinessRuleException>(
            () => ProjectPeriodRule.EnsureDateAllowed(OpenEndedProject(), Utc(2026, 2, 20)));

        Assert.Equal(ErrorCodes.ProjectDateRange, ex.Code);
    }
}