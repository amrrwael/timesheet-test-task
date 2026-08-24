using MongoDB.Driver;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Infrastructure;
using Timesheet.Api.Domain.Exceptions;

namespace Timesheet.Api.Services;

public class ReferenceService
{
    private readonly IMongoDatabase _db;

    public ReferenceService(IMongoDatabase db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken ct)
    {
        return await _db.GetCollection<Employee>(MongoCollections.Employees)
            .Find(FilterDefinition<Employee>.Empty)
            .SortBy(e => e.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken ct)
    {
        return await _db.GetCollection<Project>(MongoCollections.Projects)
            .Find(FilterDefinition<Project>.Empty)
            .SortBy(p => p.Code)
            .ToListAsync(ct);
    }

    public async Task<Employee?> GetEmployeeByIdAsync(string id, CancellationToken ct)
    {
        return await _db.GetCollection<Employee>(MongoCollections.Employees)
            .Find(e => e.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Project?> GetProjectByIdAsync(string id, CancellationToken ct)
    {
        return await _db.GetCollection<Project>(MongoCollections.Projects)
            .Find(p => p.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Добавляет ставку с даты; если ставка с такой датой начала уже есть —
    /// заменяет её (изменение ставки задним числом, сценарий приёмки №8).</summary>
    public async Task<Employee> AddRateAsync(string employeeId, DateTime fromUtcMidnight, decimal value, CancellationToken ct)
    {
        var filter = Builders<Employee>.Filter.Eq(e => e.Id, employeeId);

        var update = Builders<Employee>.Update
            .PullFilter(e => e.Rates, r => r.From == fromUtcMidnight)
            .Push(e => e.Rates, new Rate { From = fromUtcMidnight, Value = value });

        var options = new FindOneAndUpdateOptions<Employee> { ReturnDocument = ReturnDocument.After };

        var employee = await _db.GetCollection<Employee>(MongoCollections.Employees)
            .FindOneAndUpdateAsync(filter, update, options, ct);

        return employee ?? throw new NotFoundException($"Сотрудник {employeeId} не найден.");
    }
}