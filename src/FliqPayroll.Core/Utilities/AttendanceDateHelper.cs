namespace FliqPayroll.Core.Utilities;

/// <summary>
/// Calendar-date helpers for attendance stored as Philippine date-only values (no time component).
/// </summary>
public static class AttendanceDateHelper
{
    public static DateTime ToCalendarDate(DateTime value) =>
        PhilippineTime.ForDateStorage(value);

    public static bool MatchesCalendarDate(DateTime storedDate, DateTime calendarDate)
    {
        var stored = PhilippineTime.ForDateStorage(storedDate);
        var calendar = PhilippineTime.ForDateStorage(calendarDate);

        return stored.Year == calendar.Year
            && stored.Month == calendar.Month
            && stored.Day == calendar.Day;
    }

    public static bool IsWithinCalendarRange(DateTime storedDate, DateTime startDate, DateTime endDate)
    {
        var stored = PhilippineTime.ForDateStorage(storedDate);
        var start = PhilippineTime.ForDateStorage(startDate);
        var end = PhilippineTime.ForDateStorage(endDate);

        return stored >= start && stored <= end;
    }
}
