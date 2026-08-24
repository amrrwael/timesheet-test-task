using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Timesheet.Api.Domain.Entities;

public class Employee
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("department")]
    public string Department { get; set; } = null!;

    [BsonElement("rates")]
    public List<Rate> Rates { get; set; } = new();
}