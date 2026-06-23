namespace FliqPayroll.Core.Utilities;

using System.Globalization;

public static class PhilippineTime
{
    public const string TimeZoneId = "Asia/Manila";

    private static readonly TimeZoneInfo PhilippinesTimeZone = ResolvePhilippinesTimeZone();

    public static TimeZoneInfo TimeZone => PhilippinesTimeZone;

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhilippinesTimeZone);

    public static DateTime Today => Now.Date;

    public static void EnsureConfigured()
    {
        _ = PhilippinesTimeZone;
    }

    public static DateTime ConvertToPhilippineTime(DateTime utcDate)
    {
        var utc = utcDate.Kind switch
        {
            DateTimeKind.Utc => utcDate,
            DateTimeKind.Local => utcDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcDate, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, PhilippinesTimeZone);
    }

    public static DateTime ConvertToUtc(DateTime philippineDateTime)
    {
        if (philippineDateTime.Kind == DateTimeKind.Utc)
        {
            return philippineDateTime;
        }

        var local = philippineDateTime.Kind == DateTimeKind.Local
            ? TimeZoneInfo.ConvertTime(philippineDateTime, TimeZoneInfo.Local, PhilippinesTimeZone)
            : DateTime.SpecifyKind(philippineDateTime, DateTimeKind.Unspecified);

        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeToUtc(local, PhilippinesTimeZone),
            DateTimeKind.Utc);
    }

    public static DateTime ToPhilippineDate(DateTime storedDate)
    {
        if (storedDate.Kind == DateTimeKind.Utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(storedDate, PhilippinesTimeZone).Date;
        }

        if (storedDate.TimeOfDay != TimeSpan.Zero)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(storedDate, DateTimeKind.Utc),
                PhilippinesTimeZone).Date;
        }

        return storedDate.Date;
    }

    /// <summary>
    /// Persists date-only values exactly as the calendar date (no UTC offset shift).
    /// </summary>
    public static DateTime ForDateStorage(DateTime calendarDate) =>
        DateTime.SpecifyKind(ToPhilippineDate(calendarDate), DateTimeKind.Unspecified);

    public static bool TryParseCalendarDate(string? value, out DateTime calendarDate)
    {
        calendarDate = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        if (DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
        {
            calendarDate = isoDate.ToDateTime(TimeOnly.MinValue);
            return true;
        }

        var formats = new[]
        {
            "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy", "yyyy/MM/dd"
        };

        if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            calendarDate = ForDateStorage(parsed);
            return true;
        }

        if (DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
        {
            calendarDate = fallback.ToDateTime(TimeOnly.MinValue);
            return true;
        }

        return false;
    }

    public static DateTime ToUtcDate(DateTime philippineCalendarDate)
    {
        return ConvertToUtc(philippineCalendarDate.Date);
    }

    public static (DateTime StartUtc, DateTime EndUtcExclusive) ToUtcDateRange(DateTime philippineStart, DateTime philippineEnd)
    {
        var start = ToUtcDate(philippineStart.Date);
        var end = ToUtcDate(philippineEnd.Date.AddDays(1));
        return (start, end);
    }

    public static bool IsSunday(DateTime date) =>
        ToPhilippineDate(date).DayOfWeek == DayOfWeek.Sunday;

    public static string Format(DateTime value, string format = "MMM dd, yyyy")
    {
        return ConvertToPhilippineTime(
            value.Kind == DateTimeKind.Utc ? value : ConvertToUtc(value)).ToString(format);
    }

    private static TimeZoneInfo ResolvePhilippinesTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Philippines Standard Time",
                    TimeSpan.FromHours(8),
                    "Philippines Standard Time",
                    "Philippines Standard Time");
            }
        }
    }
}
