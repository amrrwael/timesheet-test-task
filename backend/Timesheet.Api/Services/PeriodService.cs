using MongoDB.Driver;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Domain.Exceptions;
using Timesheet.Api.Infrastructure;

namespace Timesheet.Api.Services;

public class PeriodService
{
    private readonly IMongoCollection<ClosedPeriod> _periods;

    public PeriodService(IMongoDatabase db)
    {
        _periods = db.GetCollection<ClosedPeriod>(MongoCollections.ClosedPeriods);
    }

    public async Task<IReadOnlyList<PeriodDto>> GetAllAsync(CancellationToken ct)
    {
        var docs = await _periods.Find(FilterDefinition<ClosedPeriod>.Empty)
            .SortBy(p => p.Year).ThenBy(p => p.Month)
            .ToListAsync(ct);

        return docs.Select(p => new PeriodDto(p.Year, p.Month, p.ClosedAt)).ToList();
    }

    public async Task<PeriodDto> CloseAsync(PeriodRequest request, CancellationToken ct)
    {
        var filter = ByYearMonth(request.Year, request.Month);

        var existing = await _periods.Find(filter).FirstOrDefaultAsync(ct);
        if (existing is not null)
            throw new ConflictException(
                ErrorCodes.PeriodAlreadyClosed,
                $"Период {request.Month:00}.{request.Year} уже закрыт.");

        var closed = new ClosedPeriod
        {
            Id = string.Empty,
            Year = request.Year,
            Month = request.Month,
            ClosedAt = DateTime.UtcNow
        };

        await _periods.InsertOneAsync(closed, cancellationToken: ct);
        return new PeriodDto(closed.Year, closed.Month, closed.ClosedAt);
    }

    public async Task OpenAsync(PeriodRequest request, CancellationToken ct)
    {
        var result = await _periods.DeleteOneAsync(ByYearMonth(request.Year, request.Month), ct);

        if (result.DeletedCount == 0)
            throw new ConflictException(
                ErrorCodes.PeriodNotClosed,
                $"Период {request.Month:00}.{request.Year} не был закрыт.");
    }

    private static FilterDefinition<ClosedPeriod> ByYearMonth(int year, int month) =>
        Builders<ClosedPeriod>.Filter.Eq(p => p.Year, year)
        & Builders<ClosedPeriod>.Filter.Eq(p => p.Month, month);
}