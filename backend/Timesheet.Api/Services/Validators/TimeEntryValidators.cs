using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using MongoDB.Bson;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Rules;

namespace Timesheet.Api.Services.Validators;

/// <summary>Валидация формы запроса (формат данных) — отделена от бизнес-правил.</summary>
public abstract class TimeEntryWriteValidator<T> : AbstractValidator<T> where T : ITimeEntryWriteRequest
{
    private static readonly Regex DateFormat =
        new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);

    protected TimeEntryWriteValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Не указан сотрудник.")
            .Must(BeValidObjectId).WithMessage("Некорректный идентификатор сотрудника.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Не указан проект.")
            .Must(BeValidObjectId).WithMessage("Некорректный идентификатор проекта.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Не указана дата записи.")
            .Must(d => DateFormat.IsMatch(d) &&
                       DateTime.TryParse(d, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("Дата должна быть в формате ГГГГ-ММ-ДД, например 2026-03-05.");

        RuleFor(x => x.Hours)
            .Must(HoursRule.IsValid)
            .WithMessage("Часы должны быть больше нуля, кратны 0,5 и не больше 24.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Комментарий не может быть длиннее 500 символов.");
    }

    private static bool BeValidObjectId(string id) => ObjectId.TryParse(id, out _);
}

public class CreateTimeEntryRequestValidator : TimeEntryWriteValidator<CreateTimeEntryRequest>;

public class UpdateTimeEntryRequestValidator : TimeEntryWriteValidator<UpdateTimeEntryRequest>
{
    public UpdateTimeEntryRequestValidator()
    {
        RuleFor(x => x.Version)
            .GreaterThan(0).WithMessage("Для изменения записи требуется её версия.");
    }
}