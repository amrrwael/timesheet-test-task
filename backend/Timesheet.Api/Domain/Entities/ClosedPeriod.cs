using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Timesheet.Api.Domain.Entities;

public class ClosedPeriod
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("month")]
    public int Month { get; set; }

    [BsonElement("closedAt")]
    public DateTime ClosedAt { get; set; }
}