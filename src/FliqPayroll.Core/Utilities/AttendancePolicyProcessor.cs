using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;

namespace FliqPayroll.Core.Utilities;

public static class AttendancePolicyProcessor
{
    public static ProcessedAttendanceDayDto? ProcessDayPunches(
        int employeeId,
        string employeeCode,
        DateTime date,
        IReadOnlyList<BiometricCsvPunchDto> dayPunches,
        IReadOnlyList<BiometricCsvPunchDto>? nextDayPunches = null,
        ShiftSchedule? shift = null)
    {
        if (dayPunches.Count == 0)
        {
            return null;
        }

        var timeIn = dayPunches
            .Where(p => p.AttendanceCode == AttendanceConstants.CodeTimeIn)
            .Select(p => (TimeSpan?)p.Time)
            .OrderBy(t => t)
            .FirstOrDefault();

        // A Time Out earlier than the Time In is the previous night's logout (overnight shifts), not this day's.
        var sameDayTimeOut = dayPunches
            .Where(p => p.AttendanceCode == AttendanceConstants.CodeTimeOut &&
                        (!timeIn.HasValue || p.Time > timeIn.Value))
            .Select(p => (TimeSpan?)p.Time)
            .OrderByDescending(t => t)
            .FirstOrDefault();

        // Prefer same-day Time Out. Otherwise accept next-day Time Out within the 6:00 AM grace.
        var timeOut = sameDayTimeOut;
        var isNextDayTimeOut = false;

        if (!timeOut.HasValue && nextDayPunches is { Count: > 0 })
        {
            var graceTimeOut = nextDayPunches
                .Where(p =>
                    p.AttendanceCode == AttendanceConstants.CodeTimeOut &&
                    p.Time <= AttendanceConstants.NextDayTimeOutGraceEnd)
                .Select(p => (TimeSpan?)p.Time)
                .OrderByDescending(t => t)
                .FirstOrDefault();

            if (graceTimeOut.HasValue)
            {
                timeOut = graceTimeOut;
                isNextDayTimeOut = true;
            }
        }

        // OT punch codes (4/5) are ignored. Attendance requires regular Time In/Out (0/1).
        if (!timeIn.HasValue || !timeOut.HasValue)
        {
            return null;
        }

        var resolvedTimeIn = timeIn.Value;
        var resolvedTimeOut = timeOut.Value;
        var isOvertimeValid = TryGetOvertimeWindow(
            resolvedTimeIn,
            resolvedTimeOut,
            isNextDayTimeOut,
            out var overtimeIn,
            out var overtimeOut,
            shift);

        return new ProcessedAttendanceDayDto
        {
            EmployeeId = employeeId,
            EmployeeCode = employeeCode,
            Date = PhilippineTime.ForDateStorage(date),
            TimeIn = resolvedTimeIn,
            TimeOut = resolvedTimeOut,
            IsLate = AttendanceConstants.IsLateTimeIn(resolvedTimeIn, shift),
            OvertimeIn = isOvertimeValid ? overtimeIn : null,
            OvertimeOut = isOvertimeValid ? overtimeOut : null,
            IsOvertimeValid = isOvertimeValid,
            IsAttendanceValid = true
        };
    }

    /// <summary>
    /// Applies the same present/OT rules used for biometric processing to manual attendance edits.
    /// Time Out earlier than Time In is treated as next-day logout within the overnight grace.
    /// </summary>
    public static void NormalizeManualAttendance(UpdateAttendanceDto dto, ShiftSchedule? shift = null)
    {
        Guard.AgainstNull(dto, nameof(dto));

        dto.IsLate = AttendanceConstants.IsLateTimeIn(dto.TimeIn, shift);

        var isNextDayTimeOut = IsNextDayTimeOut(dto.TimeIn, dto.TimeOut);
        if (isNextDayTimeOut &&
            dto.TimeOut.HasValue &&
            dto.TimeOut.Value > AttendanceConstants.NextDayTimeOutGraceEnd)
        {
            // Past 6:00 AM grace — treat as incomplete / absent for overnight pairing.
            dto.OvertimeIn = null;
            dto.OvertimeOut = null;
            dto.IsOvertimeValid = false;
            return;
        }

        if (TryGetOvertimeWindow(
                dto.TimeIn,
                dto.TimeOut,
                isNextDayTimeOut,
                out var overtimeIn,
                out var overtimeOut,
                shift))
        {
            dto.OvertimeIn = overtimeIn;
            dto.OvertimeOut = overtimeOut;
            dto.IsOvertimeValid = true;
        }
        else
        {
            dto.OvertimeIn = null;
            dto.OvertimeOut = null;
            dto.IsOvertimeValid = false;
        }
    }

    /// <summary>
    /// Present only when regular Time In and Time Out exist.
    /// </summary>
    public static bool IsPresent(TimeSpan? timeIn, TimeSpan? timeOut) =>
        timeIn.HasValue && timeOut.HasValue;

    /// <summary>
    /// True when Time Out is earlier than Time In, indicating logout on the next calendar day.
    /// </summary>
    public static bool IsNextDayTimeOut(TimeSpan? timeIn, TimeSpan? timeOut) =>
        timeIn.HasValue && timeOut.HasValue && timeOut.Value < timeIn.Value;

    /// <summary>
    /// Same-day: OT from the shift end when Time Out is the minimum overtime hours past it
    /// (5:00 PM / 7:00 PM for the Day Shift). Shifts that end after midnight have no same-day OT.
    /// Overnight (next-day Time Out within grace): OT is hours beyond the shift's continuous regular hours.
    /// </summary>
    public static bool TryGetOvertimeWindow(
        TimeSpan? timeIn,
        TimeSpan? timeOut,
        bool isNextDayTimeOut,
        out TimeSpan overtimeIn,
        out TimeSpan overtimeOut,
        ShiftSchedule? shift = null)
    {
        shift ??= ShiftSchedule.Default;
        overtimeIn = default;
        overtimeOut = default;

        if (!timeIn.HasValue || !timeOut.HasValue)
        {
            return false;
        }

        if (isNextDayTimeOut || IsNextDayTimeOut(timeIn, timeOut))
        {
            if (timeOut.Value > AttendanceConstants.NextDayTimeOutGraceEnd)
            {
                return false;
            }

            var totalHours = CalculateHoursWorked(timeIn.Value, timeOut.Value, isNextDay: true);
            if (totalHours <= shift.LengthHours)
            {
                return false;
            }

            // OT starts after the shift's continuous regular hours (9) from Time In (may wrap past midnight).
            overtimeIn = AddHoursWrapping(timeIn.Value, shift.LengthHours);
            overtimeOut = timeOut.Value;
            return true;
        }

        // Same-day late logout rule.
        if (shift.TryGetSameDayOvertimeStart(timeOut.Value, out var sameDayOvertimeStart))
        {
            overtimeIn = sameDayOvertimeStart;
            overtimeOut = timeOut.Value;
            return true;
        }

        return false;
    }

    public static bool TryGetEffectiveTimeWindow(
        TimeSpan? timeIn,
        TimeSpan? timeOut,
        out TimeSpan effectiveTimeIn,
        out TimeSpan effectiveTimeOut)
    {
        if (timeIn.HasValue && timeOut.HasValue)
        {
            effectiveTimeIn = timeIn.Value;
            effectiveTimeOut = timeOut.Value;
            return true;
        }

        effectiveTimeIn = default;
        effectiveTimeOut = default;
        return false;
    }

    public static decimal CalculateHoursWorked(TimeSpan timeIn, TimeSpan timeOut, bool isNextDay)
    {
        var duration = isNextDay || timeOut < timeIn
            ? timeOut.Add(TimeSpan.FromDays(1)) - timeIn
            : timeOut - timeIn;

        return Math.Max(0m, Math.Round((decimal)duration.TotalHours, 2, MidpointRounding.AwayFromZero));
    }

    public static decimal CalculateHoursWorked(TimeSpan? timeIn, TimeSpan? timeOut)
    {
        if (!timeIn.HasValue || !timeOut.HasValue)
        {
            return 0m;
        }

        return CalculateHoursWorked(
            timeIn.Value,
            timeOut.Value,
            IsNextDayTimeOut(timeIn, timeOut));
    }

    /// <summary>
    /// Hours worked inside 10:00 PM–6:00 AM. Overnight Time Out is the next calendar morning.
    /// </summary>
    public static decimal CalculateNightDifferentialHours(TimeSpan? timeIn, TimeSpan? timeOut)
    {
        if (!timeIn.HasValue || !timeOut.HasValue)
        {
            return 0m;
        }

        var start = ToClockMinutes(timeIn.Value);
        var end = ToClockMinutes(timeOut.Value);
        if (end < start)
        {
            end += 24 * 60;
        }

        var nightStart = ToClockMinutes(PayrollConstants.NightDifferentialStart);
        var nightEnd = ToClockMinutes(PayrollConstants.NightDifferentialEnd);
        // 12:00 AM–6:00 AM on the Time In day, and 10:00 PM through 6:00 AM the next morning.
        var minutes =
            OverlapMinutes(start, end, 0, nightEnd) +
            OverlapMinutes(start, end, nightStart, (24 * 60) + nightEnd);

        return Math.Round(minutes / 60m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateOvertimeHours(TimeSpan? timeIn, TimeSpan? timeOut, ShiftSchedule? shift = null)
    {
        var isNextDay = IsNextDayTimeOut(timeIn, timeOut);
        if (!TryGetOvertimeWindow(timeIn, timeOut, isNextDay, out var overtimeIn, out var overtimeOut, shift))
        {
            return 0m;
        }

        return CalculateHoursWorked(overtimeIn, overtimeOut, isNextDay && overtimeOut < overtimeIn);
    }

    /// <summary>
    /// Next-day Time Out punches within the 6:00 AM grace belong to the previous workday
    /// and must not create a standalone attendance day.
    /// </summary>
    public static bool IsGracePeriodTimeOutOnlyDay(IReadOnlyList<BiometricCsvPunchDto> dayPunches)
    {
        if (dayPunches.Count == 0)
        {
            return false;
        }

        var hasTimeIn = dayPunches.Any(p => p.AttendanceCode == AttendanceConstants.CodeTimeIn);
        if (hasTimeIn)
        {
            return false;
        }

        var timeOuts = dayPunches
            .Where(p => p.AttendanceCode == AttendanceConstants.CodeTimeOut)
            .Select(p => p.Time)
            .ToList();

        return timeOuts.Count > 0 &&
               timeOuts.All(t => t <= AttendanceConstants.NextDayTimeOutGraceEnd);
    }

    private static int ToClockMinutes(TimeSpan time) =>
        (time.Hours * 60) + time.Minutes;

    private static int OverlapMinutes(int start, int end, int windowStart, int windowEnd)
    {
        var overlapStart = Math.Max(start, windowStart);
        var overlapEnd = Math.Min(end, windowEnd);
        return overlapEnd > overlapStart ? overlapEnd - overlapStart : 0;
    }

    private static TimeSpan AddHoursWrapping(TimeSpan start, decimal hours)
    {
        var totalMinutes = (start.Hours * 60) + start.Minutes + (int)(hours * 60m);
        totalMinutes %= 24 * 60;
        if (totalMinutes < 0)
        {
            totalMinutes += 24 * 60;
        }

        return new TimeSpan(totalMinutes / 60, totalMinutes % 60, 0);
    }
}
