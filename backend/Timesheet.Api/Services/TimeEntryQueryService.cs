using MongoDB.Bson;
using MongoDB.Driver;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Domain.Rules;
using Timesheet.Api.Infrastructure;

namespace Timesheet.Api.Services;

/// <summary>Чтение списка записей: вся тяжёлая работа — агрегациями в MongoDB.</summary>
public class TimeEntryQueryService
{
    private readonly IMongoCollection<TimeEntry> _entries;

    public TimeEntryQueryService(IMongoDatabase db)
    {
        _entries = db.GetCollection<TimeEntry>(MongoCollections.TimeEntries);
    }

    public async Task<TimeEntriesPageDto> GetPageAsync(TimeEntriesFilter filter, CancellationToken ct)
    {
        var monthStart = DateNormalizer.Normalize($"{filter.Year:0000}-{filter.Month:00}-01");
        var monthEnd = monthStart.AddMonths(1);

        var match = TimeEntryPipeline.DateRange(monthStart, monthEnd);
        if (!string.IsNullOrEmpty(filter.EmployeeId))
            match["employeeId"] = ObjectId.Parse(filter.EmployeeId);
        if (!string.IsNullOrEmpty(filter.ProjectId))
            match["projectId"] = ObjectId.Parse(filter.ProjectId);

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            TimeEntryPipeline.Lookup(MongoCollections.Employees, "employeeId", "emp"),
            TimeEntryPipeline.Unwind("$emp"),
            TimeEntryPipeline.Lookup(MongoCollections.Projects, "projectId", "prj"),
            TimeEntryPipeline.Unwind("$prj"),
            TimeEntryPipeline.AddAppliedRateStage(),
            TimeEntryPipeline.AddCostStage(),
            new BsonDocument("$facet", new BsonDocument
            {
                { "items", new BsonArray
                  {
                      new BsonDocument("$sort", new BsonDocument { { "date", -1 }, { "_id", 1 } }),
                      new BsonDocument("$skip", (filter.Page - 1) * filter.PageSize),
                      new BsonDocument("$limit", filter.PageSize)
                  } },
                { "totals", new BsonArray
                  {
                      new BsonDocument("$group", new BsonDocument
                      {
                          { "_id", BsonNull.Value },
                          { "hours", new BsonDocument("$sum", "$hours") },
                          { "amount", new BsonDocument("$sum", "$cost") },
                          { "count", new BsonDocument("$sum", 1) }
                      })
                  } }
            })
        };

        var facet = await _entries.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(ct);
        var overtimeKeys = await GetOvertimeKeysAsync(match, ct);

        var items = new List<TimeEntryDto>();
        long totalCount = 0;
        var totalHours = 0d;
        var totalAmount = 0m;

        if (facet is not null)
        {
            foreach (var item in facet["items"].AsBsonArray)
                items.Add(MapItem(item.AsBsonDocument, overtimeKeys));

            var totals = facet["totals"].AsBsonArray.FirstOrDefault()?.AsBsonDocument;
            if (totals is not null)
            {
                totalCount = totals["count"].ToInt64();
                totalHours = totals["hours"].ToDouble();
                totalAmount = totals["amount"].ToDecimalSafe();
            }
        }

        return new TimeEntriesPageDto(items, filter.Page, filter.PageSize, totalCount, totalHours, totalAmount);
    }

    /// <summary>Ключи «сотрудник|день» с суммарными суточными часами больше 12 —
    /// по всем проектам независимо от фильтров списка.</summary>
    private async Task<HashSet<string>> GetOvertimeKeysAsync(BsonDocument monthMatch, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", monthMatch),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument { { "employeeId", "$employeeId" }, { "date", "$date" } } },
                { "totalHours", new BsonDocument("$sum", "$hours") }
            }),
            new BsonDocument("$match", new BsonDocument("totalHours",
                new BsonDocument("$gt", DailyHoursRule.OvertimeThreshold)))
        };

        var rows = await _entries.Aggregate<BsonDocument>(pipeline).ToListAsync(ct);

        return rows.Select(r =>
        {
            var employeeId = r["_id"]["employeeId"].AsObjectId.ToString();
            var day = r["_id"]["date"].ToUniversalTime();
            return $"{employeeId}|{day:yyyy-MM-dd}";
        }).ToHashSet();
    }

    private static TimeEntryDto MapItem(BsonDocument doc, HashSet<string> overtimeKeys)
    {
        var employeeId = doc["employeeId"].AsObjectId.ToString();
        var date = doc["date"].ToUniversalTime().ToString("yyyy-MM-dd");
        var comment = doc.TryGetValue("comment", out var c) && !c.IsBsonNull ? c.AsString : null;

        return new TimeEntryDto(
            Id: doc["_id"].AsObjectId.ToString(),
            EmployeeId: employeeId,
            EmployeeName: doc["emp"]["name"].AsString,
            ProjectId: doc["projectId"].AsObjectId.ToString(),
            ProjectCode: doc["prj"]["code"].AsString,
            ProjectName: doc["prj"]["name"].AsString,
            Date: date,
            Hours: doc["hours"].ToDouble(),
            Rate: doc["appliedRate"].ToDecimalSafe(),
            Amount: doc["cost"].ToDecimalSafe(),
            Comment: comment,
            Version: doc["version"].ToInt32(),
            Overtime: overtimeKeys.Contains($"{employeeId}|{date}"));
    }
}