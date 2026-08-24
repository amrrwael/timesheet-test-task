using MongoDB.Driver;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Infrastructure;

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

    public Task<Employee?> GetEmployeeByIdAsync(string id, CancellationToken ct) =>
    _db.GetCollection<Employee>(MongoCollections.Employees)
        .Find(e => e.Id == id)
        .FirstOrDefaultAsync(ct);

    public Task<Project?> GetProjectByIdAsync(string id, CancellationToken ct) =>
        _db.GetCollection<Project>(MongoCollections.Projects)
            .Find(p => p.Id == id)
            .FirstOrDefaultAsync(ct);
}