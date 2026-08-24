using MongoDB.Driver;
using Timesheet.Api.Domain.Entities;

namespace Timesheet.Api.Infrastructure;

public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(IMongoDatabase db)
    {
        var timeEntries = db.GetCollection<TimeEntry>(MongoCollections.TimeEntries);
        await timeEntries.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<TimeEntry>(
                Builders<TimeEntry>.IndexKeys.Ascending(e => e.EmployeeId).Ascending(e => e.Date),
                new CreateIndexOptions { Name = "ix_entries_employee_date" }),
            new CreateIndexModel<TimeEntry>(
                Builders<TimeEntry>.IndexKeys.Ascending(e => e.ProjectId).Ascending(e => e.Date),
                new CreateIndexOptions { Name = "ix_entries_project_date" }),
            new CreateIndexModel<TimeEntry>(
                Builders<TimeEntry>.IndexKeys.Ascending(e => e.Date),
                new CreateIndexOptions { Name = "ix_entries_date" })
        });

        var projects = db.GetCollection<Project>(MongoCollections.Projects);
        await projects.Indexes.CreateOneAsync(new CreateIndexModel<Project>(
            Builders<Project>.IndexKeys.Ascending(p => p.Code),
            new CreateIndexOptions { Unique = true, Name = "ux_projects_code" }));

        var closedPeriods = db.GetCollection<ClosedPeriod>(MongoCollections.ClosedPeriods);
        await closedPeriods.Indexes.CreateOneAsync(new CreateIndexModel<ClosedPeriod>(
            Builders<ClosedPeriod>.IndexKeys.Ascending(p => p.Year).Ascending(p => p.Month),
            new CreateIndexOptions { Unique = true, Name = "ux_closed_periods_year_month" }));
    }
}