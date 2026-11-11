using FliqPayroll.Core.Constants;

namespace FliqPayroll.Core.Utilities;

/// <summary>
/// The working window of a shift. All late / undertime / overtime rules are expressed relative to it.
/// A shift whose end is at or before its start (e.g. 5:00 PM – 2:00 AM) ends on the next calendar day.
/// </summary>
public sealed record ShiftSchedule(string Name, TimeSpan Start, TimeSpan End)
{
    private const int MinutesPerDay = 24 * 60;

    /// <summary>Day Shift (8:00 AM – 5:00 PM). Used when no shift data is available at all.</summary>
    public static ShiftSchedule Default { get; } =
        new(ShiftConstants.DefaultShiftName, AttendanceConstants.WorkStart, AttendanceConstants.WorkEnd);

    public int StartMinutes => ToClockMinutes(Start);

    public int EndMinutes => ToClockMinutes(End);

    public bool CrossesMidnight => EndMinutes <= StartMinutes;

    public int LengthMinutes
    {
        get
        {
            var minutes = (EndMinutes - StartMinutes + MinutesPerDay) % MinutesPerDay;
            return minutes == 0 ? MinutesPerDay : minutes;
        }
    }

    /// <summary>Scheduled continuous hours (9 for both standard shifts).</summary>
    public decimal LengthHours => LengthMinutes / 60m;

    public int MinutesAfterStart(TimeSpan time) => ToTimeline(time) - StartMinutes;

    public bool IsLate(TimeSpan? timeIn) =>
        timeIn.HasValue && MinutesAfterStart(timeIn.Value) > AttendanceConstants.GracePeriodMinutes;

    /// <summary>Minutes after the shift start (grace period exhausted ⇒ the full lateness is deducted).</summary>
    public decimal CalculateLateMinutes(TimeSpan? timeIn) =>
        IsLate(timeIn) ? MinutesAfterStart(timeIn!.Value) : 0m;

    /// <summary>Minutes between the actual Time Out and the scheduled shift end, when leaving early.</summary>
    public decimal CalculateUndertimeMinutes(TimeSpan? timeIn, TimeSpan? timeOut)
    {
        if (!timeIn.HasValue || !timeOut.HasValue)
        {
            return 0m;
        }

        var timelineIn = ToTimeline(timeIn.Value);
        var timelineOut = ToTimeline(timeOut.Value);
        if (timelineOut < timelineIn)
        {
            // Logout on the next calendar day.
            timelineOut += MinutesPerDay;
        }

        return Math.Max(0, TimelineEnd - timelineOut);
    }

    /// <summary>
    /// Same-day overtime: only for shifts that end before midnight, and only once the employee
    /// stays the minimum overtime hours past the shift end (5:00 PM → 7:00 PM for the Day Shift).
    /// </summary>
    public bool TryGetSameDayOvertimeStart(TimeSpan timeOut, out TimeSpan overtimeStart)
    {
        overtimeStart = End;
        if (CrossesMidnight)
        {
            return false;
        }

        var threshold = EndMinutes + (int)(AttendanceConstants.MinimumOvertimeHours * 60m);
        return threshold < MinutesPerDay && ToClockMinutes(timeOut) >= threshold;
    }

    private int TimelineEnd => CrossesMidnight ? EndMinutes + MinutesPerDay : EndMinutes;

    /// <summary>
    /// Position on the shift's own timeline. For overnight shifts, clock times at or before the
    /// shift end belong to the second calendar day.
    /// </summary>
    private int ToTimeline(TimeSpan time)
    {
        var minutes = ToClockMinutes(time);
        return CrossesMidnight && minutes <= EndMinutes ? minutes + MinutesPerDay : minutes;
    }

    private static int ToClockMinutes(TimeSpan time) => (time.Hours * 60) + time.Minutes;
}
