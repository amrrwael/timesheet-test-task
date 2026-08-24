using MongoDB.Driver;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Domain.Exceptions;
using Timesheet.Api.Domain.Rules;
using Timesheet.Api.Infrastructure;

namespace Timesheet.Api.Services;

public class TimeEntryService
{
    private readonly IMongoDatabase _db;
    private readonly ReferenceService _reference;
    private readonly IMongoCollection<TimeEntry> _entries;

    public TimeEntryService(IMongoDatabase db, ReferenceService reference)
    {
        _db = db;
        _reference = reference;
        _entries = db.GetCollection<TimeEntry>(MongoCollections.TimeEntries);
    }

    public async Task<TimeEntryDto> CreateAsync(CreateTimeEntryRequest request, string createdBy, CancellationToken ct)
    {
        var date = DateNormalizer.Normalize(request.Date);

        var employee = await _reference.GetEmployeeByIdAsync(request.EmployeeId, ct)
            ?? throw new NotFoundException($"Сотрудник {request.EmployeeId} не найден.");
        var project = await _reference.GetProjectByIdAsync(request.ProjectId, ct)
            ?? throw new NotFoundException($"Проект {request.ProjectId} не найден.");

        await EnsurePeriodOpenAsync(date, ct);

        var rate = RateResolver.Resolve(employee.Rates, date)
            ?? throw new BusinessRuleException(
                ErrorCodes.RateNotFound,
                $"На {date:dd.MM.yyyy} у сотрудника «{employee.Name}» ещё нет часовой ставки.");

        ProjectPeriodRule.EnsureDateAllowed(project, date);

        var otherHoursToday = await GetEmployeeDayTotalAsync(employee.Id, date, excludeEntryId: null, ct);
        DailyHoursRule.EnsureDayTotalAllowed(otherHoursToday, request.Hours);

        var entry = new TimeEntry
        {
            Id = string.Empty, // драйвер сам сгенерирует ObjectId при вставке
            EmployeeId = employee.Id,
            ProjectId = project.Id,
            Date = date,
            Hours = request.Hours,
            Comment = request.Comment,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        await _entries.InsertOneAsync(entry, cancellationToken: ct);

        return BuildDto(entry, employee.Name, project.Code, project.Name, rate,
            DailyHoursRule.IsOvertime(otherHoursToday + request.Hours));
    }

    public async Task<TimeEntryDto> UpdateAsync(string id, UpdateTimeEntryRequest request, string updatedBy, CancellationToken ct)
    {
        var existing = await _entries.Find(e => e.Id == id).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Запись табеля {id} не найдена.");

        // закрыт ли период, где запись лежит сейчас, и период новой даты
        await EnsurePeriodOpenAsync(existing.Date, ct);
        var newDate = DateNormalizer.Normalize(request.Date);
        await EnsurePeriodOpenAsync(newDate, ct);

        var employee = await _reference.GetEmployeeByIdAsync(request.EmployeeId, ct)
            ?? throw new NotFoundException($"Сотрудник {request.EmployeeId} не найден.");
        var project = await _reference.GetProjectByIdAsync(request.ProjectId, ct)
            ?? throw new NotFoundException($"Проект {request.ProjectId} не найден.");

        var rate = RateResolver.Resolve(employee.Rates, newDate)
            ?? throw new BusinessRuleException(
                ErrorCodes.RateNotFound,
                $"На {newDate:dd.MM.yyyy} у сотрудника «{employee.Name}» ещё нет часовой ставки.");

        ProjectPeriodRule.EnsureDateAllowed(project, newDate);

        var otherHoursThatDay = await GetEmployeeDayTotalAsync(employee.Id, newDate, excludeEntryId: existing.Id, ct);
        DailyHoursRule.EnsureDayTotalAllowed(otherHoursThatDay, request.Hours);

        existing.EmployeeId = employee.Id;
        existing.ProjectId = project.Id;
        existing.Date = newDate;
        existing.Hours = request.Hours;
        existing.Comment = request.Comment;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = updatedBy;
        existing.Version = request.Version + 1;

        // оптимистическая блокировка: меняем документ только если версия не ушла
        var filter = Builders<TimeEntry>.Filter.Eq(e => e.Id, id)
                   & Builders<TimeEntry>.Filter.Eq(e => e.Version, request.Version);

        var result = await _entries.ReplaceOneAsync(filter, existing, cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            var stillExists = await _entries.Find(e => e.Id == id).AnyAsync(ct);
            if (!stillExists)
                throw new NotFoundException($"Запись табеля {id} не найдена.");

            throw new ConflictException(
                ErrorCodes.VersionConflict,
                "Запись изменена кем-то после того, как вы её открыли. Обновите список и повторите правку.");
        }

        return BuildDto(existing, employee.Name, project.Code, project.Name, rate,
            DailyHoursRule.IsOvertime(otherHoursThatDay + request.Hours));
    }

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        var existing = await _entries.Find(e => e.Id == id).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Запись табеля {id} не найдена.");

        await EnsurePeriodOpenAsync(existing.Date, ct);

        await _entries.DeleteOneAsync(e => e.Id == id, ct);
    }

    /// <summary>Сумма часов сотрудника за дату. Выборка ограничена одним днём
    /// (индекс ix_entries_employee_date), поэтому суммирование в памяти безопасно;
    /// отчёты по миллионам записей считаются агрегацией на стороне MongoDB.</summary>
    private async Task<double> GetEmployeeDayTotalAsync(string employeeId, DateTime date, string? excludeEntryId, CancellationToken ct)
    {
        var filter = Builders<TimeEntry>.Filter.Eq(e => e.EmployeeId, employeeId)
                   & Builders<TimeEntry>.Filter.Eq(e => e.Date, date);

        if (excludeEntryId is not null)
            filter &= Builders<TimeEntry>.Filter.Ne(e => e.Id, excludeEntryId);

        var sameDay = await _entries.Find(filter).ToListAsync(ct);
        return sameDay.Sum(e => e.Hours);
    }

    private async Task EnsurePeriodOpenAsync(DateTime date, CancellationToken ct)
    {
        var closed = await _db.GetCollection<ClosedPeriod>(MongoCollections.ClosedPeriods)
            .Find(p => p.Year == date.Year && p.Month == date.Month)
            .AnyAsync(ct);

        ClosedPeriodGuard.EnsureOpen(closed, date.Year, date.Month);
    }

    private static TimeEntryDto BuildDto(
        TimeEntry entry, string employeeName, string projectCode, string projectName,
        decimal rate, bool overtime) => new(
            entry.Id,
            entry.EmployeeId,
            employeeName,
            entry.ProjectId,
            projectCode,
            projectName,
            entry.Date.ToString("yyyy-MM-dd"),
            entry.Hours,
            rate,
            MoneyCalculator.Cost(entry.Hours, rate),
            entry.Comment,
            entry.Version,
            overtime);
}