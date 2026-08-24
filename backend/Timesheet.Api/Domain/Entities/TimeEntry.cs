using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Timesheet.Api.Domain.Entities;

public class TimeEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("employeeId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EmployeeId { get; set; } = null!;

    [BsonElement("projectId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = null!;

    [BsonElement("date")]
    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime Date { get; set; }

    [BsonElement("hours")]
    public double Hours { get; set; }

    [BsonElement("comment")]
    public string? Comment { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("createdBy")]
    public string? CreatedBy { get; set; }

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("updatedBy")]
    public string? UpdatedBy { get; set; }
}