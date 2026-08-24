using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Timesheet.Api.Domain.Entities;

public class Rate
{
    [BsonElement("from")]
    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime From { get; set; }

    [BsonElement("value")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Value { get; set; }
}