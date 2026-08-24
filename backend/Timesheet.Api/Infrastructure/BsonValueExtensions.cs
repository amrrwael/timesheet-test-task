using MongoDB.Bson;

namespace Timesheet.Api.Infrastructure;

public static class BsonValueExtensions
{
    public static decimal ToDecimalSafe(this BsonValue value) =>
        value.IsBsonNull ? 0m : (decimal)((BsonDecimal128)value).Value;

    public static decimal? ToNullableDecimal(this BsonValue value) =>
        value.IsBsonNull ? null : (decimal)((BsonDecimal128)value).Value;
}