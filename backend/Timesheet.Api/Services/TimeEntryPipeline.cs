using MongoDB.Bson;

namespace Timesheet.Api.Services;

/// <summary>
/// Общие стадии агрегаций по time_entries: диапазон месяца, соединения со справочниками,
/// ставка, действовавшая на дату записи, стоимость записи.
/// Один источник логики для списка табеля и отчёта по проектам.
/// </summary>
public static class TimeEntryPipeline
{
    /// <summary>Условие попадания календарной даты в месяц [start, end).</summary>
    public static BsonDocument DateRange(DateTime startInclusiveUtc, DateTime endExclusiveUtc) =>
        new BsonDocument("date", new BsonDocument
        {
            { "$gte", new BsonDateTime(startInclusiveUtc) },
            { "$lt", new BsonDateTime(endExclusiveUtc) }
        });

    public static BsonDocument Lookup(string from, string localField, string asField) =>
        new("$lookup", new BsonDocument
        {
            { "from", from },
            { "localField", localField },
            { "foreignField", "_id" },
            { "as", asField }
        });

    public static BsonDocument Unwind(string path) => new("$unwind", path);

    /// <summary>Ставка, действовавшая НА дату записи: из ставок сотрудника с from &lt;= date
    /// берём последнюю по дате начала. Ставки можно менять задним числом —
    /// пересчёт происходит прямо в запросе.</summary>
    public static BsonDocument AddAppliedRateStage() =>
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

    public static BsonDocument AddCostStage() =>
        new("$addFields", new BsonDocument("cost",
            new BsonDocument("$round", new BsonArray
            {
                new BsonDocument("$multiply", new BsonArray { "$hours", "$appliedRate" }),
                2
            })));
}