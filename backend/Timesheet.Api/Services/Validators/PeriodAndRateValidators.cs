using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Timesheet.Api.Contracts;

namespace Timesheet.Api.Services.Validators;

public class PeriodRequestValidator : AbstractValidator<PeriodRequest>
{
    public PeriodRequestValidator()
    {
        RuleFor(p => p.Year).InclusiveBetween(1990, 2100)
            .WithMessage("Год должен быть в диапазоне 1990–2100.");
        RuleFor(p => p.Month).InclusiveBetween(1, 12)
            .WithMessage("Месяц должен быть от 1 до 12.");
    }
}

public class AddRateRequestValidator : AbstractValidator<AddRateRequest>
{
    private static readonly Regex DateFormat =
        new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);

    public AddRateRequestValidator()
    {
        RuleFor(r => r.From)
            .NotEmpty().WithMessage("Не указана дата начала ставки.")
            .Must(d => DateFormat.IsMatch(d) &&
                       DateTime.TryParse(d, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("Дата должна быть в формате ГГГГ-ММ-ДД, например 2026-03-01.");

        RuleFor(r => r.Value)
            .GreaterThan(0).WithMessage("Ставка должна быть положительным числом.");
    }
}