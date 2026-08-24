using FluentValidation;
using MongoDB.Bson;
using Timesheet.Api.Contracts;

namespace Timesheet.Api.Services.Validators;

public class TimeEntriesFilterValidator : AbstractValidator<TimeEntriesFilter>
{
    public TimeEntriesFilterValidator()
    {
        RuleFor(f => f.Year).InclusiveBetween(1990, 2100)
            .WithMessage("Год должен быть в диапазоне 1990–2100.");
        RuleFor(f => f.Month).InclusiveBetween(1, 12)
            .WithMessage("Месяц должен быть от 1 до 12.");
        RuleFor(f => f.Page).GreaterThanOrEqualTo(1)
            .WithMessage("Номер страницы — не меньше 1.");
        RuleFor(f => f.PageSize).InclusiveBetween(1, 100)
            .WithMessage("Размер страницы — от 1 до 100 записей.");
        RuleFor(f => f.EmployeeId)
            .Must(id => string.IsNullOrEmpty(id) || ObjectId.TryParse(id, out _))
            .WithMessage("Некорректный идентификатор сотрудника.");
        RuleFor(f => f.ProjectId)
            .Must(id => string.IsNullOrEmpty(id) || ObjectId.TryParse(id, out _))
            .WithMessage("Некорректный идентификатор проекта.");
    }
}