using MongoDB.Bson;
using MongoDB.Driver;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Domain.Rules;
using Timesheet.Api.Infrastructure;

namespace Timesheet.Api.Services;

public class ReportService
{
    private readonly IMongoCollection<TimeEntry> _entries;

    public ReportService(IMongoDatabase db)
    {
        _entries = db.GetCollection<TimeEntry>(MongoCollections.TimeEntries);
    }

    /// <summary>Отчёт считается целиком агрегацией MongoDB: даже при миллионах записей
    /// в приложение уходит только одна строка на проект плюс итог.</summary>
    public async Task<ProjectReportDto> GetProjectReportAsync(ProjectReportFilter filter, CancellationToken ct)
    {
        var monthStart = DateNormalizer.Normalize($"{filter.Year:0000}-{filter.Month:00}-01");
        var monthEnd = monthStart.AddMonths(1);

        var pipeline = new[]
        {
            new BsonDocument("$match", TimeEntryPipeline.DateRange(monthStart, monthEnd)),
            TimeEntryPipeline.Lookup(MongoCollections.Employees, "employeeId", "emp"),
            TimeEntryPipeline.Unwind("$emp"),
            TimeEntryPipeline.Lookup(MongoCollections.Projects, "projectId", "prj"),
            TimeEntryPipeline.Unwind("$prj"),
            TimeEntryPipeline.AddAppliedRateStage(),
            TimeEntryPipeline.AddCostStage(),
            new BsonDocument("$facet", new BsonDocument
            {
                { "projects", new BsonArray
                  {
                      GroupByProjectStage(),
                      ProjectWithPercentStage(),
                      new BsonDocument("$sort", new BsonDocument("_id.code", 1))
                  } },
                { "total", new BsonArray
                  {
                      new BsonDocument("$group", new BsonDocument
                      {
                          { "_id", BsonNull.Value },
                          { "hours", new BsonDocument("$sum", "$hours") },
                          { "amount", new BsonDocument("$sum", "$cost") }
                      })
                  } }
            })
        };

        var facet = await _entries.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(ct);

        var rows = new List<ProjectReportRowDto>();
        var totalHours = 0d;
        var totalAmount = 0m;

        if (facet is not null)
        {
            foreach (var doc in facet["projects"].AsBsonArray)
                rows.Add(MapRow(doc.AsBsonDocument));

            var total = facet["total"].AsBsonArray.FirstOrDefault()?.AsBsonDocument;
            if (total is not null)
            {
                // $group с константным _id всегда возвращает одну строку:
                // при пустом месяце суммы просто равны нулю
                totalHours = total["hours"].ToDouble();
                totalAmount = total["amount"].ToDecimalSafe();
            }
        }

        return new ProjectReportDto(rows, totalHours, totalAmount);
    }

    private static BsonDocument GroupByProjectStage() =>
        new("$group", new BsonDocument
        {
            { "_id", new BsonDocument
              {
                  { "projectId", "$projectId" },
                  { "code", "$prj.code" },
                  { "name", "$prj.name" },
                  { "budget", "$prj.budget" }
              } },
            { "hours", new BsonDocument("$sum", "$hours") },
            { "amount", new BsonDocument("$sum", "$cost") }
        });

    private static BsonDocument ProjectWithPercentStage() =>
        new("$project", new BsonDocument
        {
            { "projectId", "$_id.projectId" },
            { "code", "$_id.code" },
            { "name", "$_id.name" },
            { "budget", "$_id.budget" },
            { "hours", 1 },
            { "amount", 1 },
            // процент освоения бюджета; при нулевом бюджете — null (в UI «—»),
            // деление на ноль никогда не выполняется
            { "percent", new BsonDocument("$let", new BsonDocument
              {
                  { "vars", new BsonDocument("budget", "$_id.budget") },
                  { "in", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$gt", new BsonArray { "$$budget", 0 }),
                        new BsonDocument("$round", new BsonArray
                        {
                            new BsonDocument("$multiply", new BsonArray
                            {
                                new BsonDocument("$divide", new BsonArray { "$amount", "$$budget" }),
                                100
                            }),
                            2
                        }),
                        BsonNull.Value
                    }) }
              }) }
        });

    private static ProjectReportRowDto MapRow(BsonDocument doc)
    {
        var percent = doc["percent"].ToNullableDecimal();

        return new ProjectReportRowDto(
            ProjectId: doc["projectId"].AsObjectId.ToString(),
            Code: doc["code"].AsString,
            Name: doc["name"].AsString,
            Budget: doc["budget"].ToDecimalSafe(),
            Hours: Math.Round(doc["hours"].ToDouble(), 2),
            Amount: doc["amount"].ToDecimalSafe(),
            Percent: percent,
            Overspent: percent > BudgetRule.OverspendThresholdPercent,
            AtRisk: percent > BudgetRule.RiskThresholdPercent &&
                    percent <= BudgetRule.OverspendThresholdPercent);
    }
}