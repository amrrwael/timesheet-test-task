using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Domain.Exceptions;

namespace Timesheet.Api.Domain.Rules;

/// <summary>
/// Дата записи должна попадать в период проекта: не раньше начала и
/// не позже окончания, если оно задано (бессрочный проект — без верхней границы).
/// Границы включительны: запись ровно на дату начала/окончания допустима.
/// </summary>
public static class ProjectPeriodRule
{
    public static void EnsureDateAllowed(Project project, DateTime dateUtcMidnight)
    {
        var withinStart = dateUtcMidnight >= project.StartDate;
        var withinEnd = !project.EndDate.HasValue || dateUtcMidnight <= project.EndDate.Value;

        if (withinStart && withinEnd)
            return;

        var range = project.EndDate.HasValue
            ? $"с {project.StartDate:dd.MM.yyyy} по {project.EndDate.Value:dd.MM.yyyy}"
            : $"с {project.StartDate:dd.MM.yyyy}, без даты окончания";

        throw new BusinessRuleException(
            ErrorCodes.ProjectDateRange,
            $"Дата {dateUtcMidnight:dd.MM.yyyy} не входит в период проекта «{project.Code}» ({range}).");
    }
}