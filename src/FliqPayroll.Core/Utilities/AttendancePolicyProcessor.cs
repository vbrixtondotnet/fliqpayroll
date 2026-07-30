using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;

namespace FliqPayroll.Core.Utilities;

public static class AttendancePolicyProcessor
{
    public static ProcessedAttendanceDayDto? ProcessDayPunches(
        int employeeId,
        string employeeCode,
        DateTime date,
        IReadOnlyList<BiometricCsvPunchDto> punches)
    {
        if (punches.Count == 0)
        {
            return null;
        }

        var timeIn = punches
            .Where(p => p.AttendanceCode == AttendanceConstants.CodeTimeIn)
            .Select(p => (TimeSpan?)p.Time)
            .OrderBy(t => t)
            .FirstOrDefault();

        var timeOut = punches
            .Where(p => p.AttendanceCode == AttendanceConstants.CodeTimeOut)
            .Select(p => (TimeSpan?)p.Time)
            .OrderByDescending(t => t)
            .FirstOrDefault();

        // OT punch codes (4/5) are ignored. Attendance requires regular Time In/Out (0/1).
        if (!timeIn.HasValue || !timeOut.HasValue)
        {
            return null;
        }

        var resolvedTimeIn = timeIn.Value;
        var resolvedTimeOut = timeOut.Value;
        var isOvertimeValid = TryGetOvertimeWindow(
            resolvedTimeOut,
            out var overtimeIn,
            out var overtimeOut);

        return new ProcessedAttendanceDayDto
        {
            EmployeeId = employeeId,
            EmployeeCode = employeeCode,
            Date = PhilippineTime.ForDateStorage(date),
            TimeIn = resolvedTimeIn,
            TimeOut = resolvedTimeOut,
            IsLate = AttendanceConstants.IsLateTimeIn(resolvedTimeIn),
            OvertimeIn = isOvertimeValid ? overtimeIn : null,
            OvertimeOut = isOvertimeValid ? overtimeOut : null,
            IsOvertimeValid = isOvertimeValid,
            IsAttendanceValid = true
        };
    }

    /// <summary>
    /// Applies the same present/OT rules used for biometric processing to manual attendance edits.
    /// </summary>
    public static void NormalizeManualAttendance(UpdateAttendanceDto dto)
    {
        Guard.AgainstNull(dto, nameof(dto));

        dto.IsLate = AttendanceConstants.IsLateTimeIn(dto.TimeIn);

        // Ignore manually supplied OT values and derive OT solely from regular Time Out.
        if (TryGetOvertimeWindow(dto.TimeOut, out var overtimeIn, out var overtimeOut))
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
    /// OT is derived from the 5:00 PM work end and applies only when Time Out is 7:00 PM or later.
    /// </summary>
    public static bool TryGetOvertimeWindow(
        TimeSpan? timeOut,
        out TimeSpan overtimeIn,
        out TimeSpan overtimeOut)
    {
        if (timeOut.HasValue && timeOut.Value >= AttendanceConstants.OvertimeThreshold)
        {
            overtimeIn = AttendanceConstants.WorkEnd;
            overtimeOut = timeOut.Value;
            return true;
        }

        overtimeIn = default;
        overtimeOut = default;
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
}
