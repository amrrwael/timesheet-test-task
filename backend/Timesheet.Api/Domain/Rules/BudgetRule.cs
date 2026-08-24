namespace Timesheet.Api.Domain.Rules;

/// <summary>Пороги освоения бюджета для отчёта по проектам.</summary>
public static class BudgetRule
{
    public const decimal RiskThresholdPercent = 80;
    public const decimal OverspendThresholdPercent = 100;
}