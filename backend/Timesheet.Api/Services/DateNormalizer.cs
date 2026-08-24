using System.Globalization;

namespace Timesheet.Api.Services;

public static class DateNormalizer
{
    /// <summary>"2026-03-05" → полночь этой даты в UTC (каноническое хранение календарных дат).</summary>
    public static DateTime Normalize(string isoDate) =>
        DateTime.SpecifyKind(DateTime.Parse(isoDate, CultureInfo.InvariantCulture), DateTimeKind.Utc);
}