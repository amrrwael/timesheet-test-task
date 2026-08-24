using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Domain.Rules;
using Timesheet.Api.Infrastructure;

namespace Timesheet.Api.Services;

/// <summary>Чтение списка записей: вся тяжёлая работа — агрегацией в MongoDB.</summary>
public class TimeEntryQueryService
{
    private readonly IMongoCollection<TimeEntry> _entries;

    public TimeEntryQueryService(IMongoDatabase db)
    {
        _entries = db.GetCollection<TimeEntry>(MongoCollections.TimeEntries);
    }

    public async Task<TimeEntriesPageDto> GetPageAsync(TimeEntriesFilter filter, CancellationToken ct)
    {
        var monthStart = DateNormalizer.Normalize(
            $"{filter.Year:0000}-{filter.Month:00}-01");
        var monthEnd = monthStart.AddMonths(1);

        var match = BuildMonthMatch(filter, monthStart, monthEnd);

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            Lookup("employees", "employeeId", "emp"),
            new BsonDocument("$unwind", "$emp"),
            Lookup("projects", "projectId", "prj"),
            new BsonDocument("$unwind", "$prj"),
            AddAppliedRateStage(),
            AddCostStage(),
            new BsonDocument("$facet", new BsonDocument
            {
                // страница данных — сортировка и отсечка выполняются сервером БД
                { "items", new BsonArray
                  {
                      new BsonDocument("$sort", new BsonDocument { { "date", -1 }, { "_id", 1 } }),
                      new BsonDocument("$skip", (filter.Page - 1) * filter.PageSize),
                      new BsonDocument("$limit", filter.PageSize)
                  } },
                // итоги по всей отфильтрованной выборке — тем же запросом
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
                totalAmount = ToDecimal(totals["amount"]);
            }
        }

        return new TimeEntriesPageDto(items, filter.Page, filter.PageSize, totalCount, totalHours, totalAmount);
    }

    /// <summary>Ключи «сотрудник|день», где суммарно за день у сотрудника больше 12 часов
    /// по всем проектам. Считается независимо от фильтров списка: переработка — факт
    /// о дне сотрудника целиком.</summary>
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

    private static BsonDocument BuildMonthMatch(TimeEntriesFilter f, DateTime start, DateTime end)
    {
        var doc = new BsonDocument
        {
            { "date", new BsonDocument { { "$gte", new BsonDateTime(start) }, { "$lt", new BsonDateTime(end) } } }
        };

        if (!string.IsNullOrEmpty(f.EmployeeId))
            doc["employeeId"] = ObjectId.Parse(f.EmployeeId);

        if (!string.IsNullOrEmpty(f.ProjectId))
            doc["projectId"] = ObjectId.Parse(f.ProjectId);

        return doc;
    }

    /// <summary>$lookup по справочникам. $unwind — внутреннее соединение: записи без
    /// справочника не бывают (проверяется при создании), «осиротевшие» документы
    /// в отчёте бессмысленны.</summary>
    private static BsonDocument Lookup(string from, string localField, string asField) =>
        new("$lookup", new BsonDocument
        {
            { "from", from },
            { "localField", localField },
            { "foreignField", "_id" },
            { "as", asField }
        });

    /// <summary>Ставка, действовавшая НА дату записи: среди ставок сотрудника берём те,
    /// что начались не позже записи, сортируем по дате начала и берём последнюю.</summary>
    private static BsonDocument AddAppliedRateStage() =>
        new("$addFields", new BsonDocument("appliedRate",
            new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument("effectiveRates",
                    new BsonDocument("$sortArray", new BsonDocument
                    {
                        { "input", new BsonDocument("$filter", new BsonDocument
                          {
                              { "input", "$emp.rates" },
                              { "as", "r" },
                              { "cond", new BsonDocument("$lte", new BsonArray { "$$r.from", "$date" }) }
                          }) },
                        { "sortBy", new BsonDocument("from", 1) }
                    })) },
                { "in", new BsonDocument("$cond", new BsonArray
                  {
                      new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$$effectiveRates"), 0 }),
                      new BsonDocument("$arrayElemAt", new BsonArray { "$$effectiveRates.value", -1 }),
                      BsonNull.Value
                  }) }
            })));

    private static BsonDocument AddCostStage() =>
        new("$addFields", new BsonDocument("cost",
            new BsonDocument("$round", new BsonArray
            {
                new BsonDocument("$multiply", new BsonArray { "$hours", "$appliedRate" }),
                2
            })));

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
            Rate: ToDecimal(doc["appliedRate"]),
            Amount: ToDecimal(doc["cost"]),
            Comment: comment,
            Version: doc["version"].ToInt32(),
            Overtime: overtimeKeys.Contains($"{employeeId}|{date}"));
    }

    private static decimal ToDecimal(BsonValue value) =>
        value.IsBsonNull ? 0m : (decimal)((BsonDecimal128)value).Value;
}