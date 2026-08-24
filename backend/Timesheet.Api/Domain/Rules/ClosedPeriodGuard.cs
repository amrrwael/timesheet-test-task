using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Exceptions;

namespace Timesheet.Api.Domain.Rules;

/// <summary>
/// В закрытом бухгалтерией периоде записи нельзя создавать, изменять и удалять.
/// Сам факт закрытия определяет сервис запросом в коллекцию closed_periods;
/// правило отвечает за понятную ошибку.
/// </summary>
public static class ClosedPeriodGuard
{
    public static void EnsureOpen(bool isClosed, int year, int month)
    {
        if (!isClosed)
            return;

        throw new ConflictException(
            ErrorCodes.PeriodClosed,
            $"Период {month:00}.{year} закрыт бухгалтерией: " +
            "создание, изменение и удаление записей запрещено.");
    }
}