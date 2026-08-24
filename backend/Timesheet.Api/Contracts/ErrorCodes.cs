namespace Timesheet.Api.Contracts;

public static class ErrorCodes
{
    public const string InternalError = "INTERNAL_ERROR";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string RateNotFound = "RATE_NOT_FOUND";
    public const string DailyLimitExceeded = "DAILY_LIMIT_EXCEEDED";
    public const string PeriodClosed = "PERIOD_CLOSED";
    public const string ProjectDateRange = "PROJECT_DATE_RANGE";
    public const string VersionConflict = "VERSION_CONFLICT";
    public const string InvalidPeriod = "INVALID_PERIOD";
    public const string PeriodAlreadyClosed = "PERIOD_ALREADY_CLOSED";
    public const string PeriodNotClosed = "PERIOD_NOT_CLOSED";
}