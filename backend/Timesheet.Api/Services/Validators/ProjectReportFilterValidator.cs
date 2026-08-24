using FluentValidation;
using Timesheet.Api.Contracts;

namespace Timesheet.Api.Services.Validators;

public class ProjectReportFilterValidator : AbstractValidator<ProjectReportFilter>
{
    public ProjectReportFilterValidator()
    {
        RuleFor(f => f.Year).InclusiveBetween(1990, 2100)
            .WithMessage("Год должен быть в диапазоне 1990–2100.");
        RuleFor(f => f.Month).InclusiveBetween(1, 12)
            .WithMessage("Месяц должен быть от 1 до 12.");
    }
}