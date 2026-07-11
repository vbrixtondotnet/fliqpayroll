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

        var overtimeIn = punches
            .Where(p => p.AttendanceCode == AttendanceConstants.CodeOvertimeIn)
            .Select(p => (TimeSpan?)p.Time)
            .OrderBy(t => t)
            .FirstOrDefault();

        var overtimeOut = punches
            .Where(p => p.AttendanceCode == AttendanceConstants.CodeOvertimeOut)
            .Select(p => (TimeSpan?)p.Time)
            .OrderByDescending(t => t)
            .FirstOrDefault();

        var hasTimeIn = timeIn.HasValue;
        var hasTimeOut = timeOut.HasValue;
        var isOvertimeValid = overtimeIn.HasValue && overtimeOut.HasValue;

        // Scenario 1: Time In + Time Out → Present; incomplete OT (e.g. OT Out only) is ignored.
        // Scenario 2: Time In + OT In + OT Out (no regular Time Out) → Present via First/Last Bio.
        var hasRegularPair = hasTimeIn && hasTimeOut;
        var hasOtContinuation = hasTimeIn && isOvertimeValid;

        if (!hasRegularPair && !hasOtContinuation)
        {
            return null;
        }

        TimeSpan resolvedTimeIn;
        TimeSpan resolvedTimeOut;

        if (hasRegularPair)
        {
            resolvedTimeIn = timeIn!.Value;
            resolvedTimeOut = timeOut!.Value;
        }
        else
        {
            // First Bio / Last Bio across all punches for the day.
            resolvedTimeIn = punches.Min(p => p.Time);
            resolvedTimeOut = punches.Max(p => p.Time);
        }

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

        var isOvertimeValid = dto.OvertimeIn.HasValue && dto.OvertimeOut.HasValue;

        // Scenario 1: ignore OT Out (or any incomplete OT pair) when OT In is missing.
        if (!isOvertimeValid)
        {
            dto.OvertimeIn = null;
            dto.OvertimeOut = null;
            dto.IsOvertimeValid = false;
        }
        else
        {
            dto.IsOvertimeValid = true;
        }

        if (dto.TimeIn.HasValue && dto.TimeOut.HasValue)
        {
            dto.IsLate = AttendanceConstants.IsLateTimeIn(dto.TimeIn);
            return;
        }

        // Scenario 2: Time In + valid OT pair without Time Out → First/Last Bio from available punches.
        if (dto.TimeIn.HasValue && isOvertimeValid)
        {
            var times = new[] { dto.TimeIn.Value, dto.OvertimeIn!.Value, dto.OvertimeOut!.Value };
            dto.TimeIn = times.Min();
            dto.TimeOut = times.Max();
            dto.IsLate = AttendanceConstants.IsLateTimeIn(dto.TimeIn);
        }
    }

    /// <summary>
    /// Present when regular Time In/Out exist, or when Time In continues into a complete OT pair.
    /// </summary>
    public static bool IsPresent(
        TimeSpan? timeIn,
        TimeSpan? timeOut,
        TimeSpan? overtimeIn,
        TimeSpan? overtimeOut) =>
        (timeIn.HasValue && timeOut.HasValue) ||
        (timeIn.HasValue && overtimeIn.HasValue && overtimeOut.HasValue);

    public static bool HasValidOvertime(TimeSpan? overtimeIn, TimeSpan? overtimeOut) =>
        overtimeIn.HasValue && overtimeOut.HasValue;

    public static bool TryGetEffectiveTimeWindow(
        TimeSpan? timeIn,
        TimeSpan? timeOut,
        TimeSpan? overtimeIn,
        TimeSpan? overtimeOut,
        out TimeSpan effectiveTimeIn,
        out TimeSpan effectiveTimeOut)
    {
        if (timeIn.HasValue && timeOut.HasValue)
        {
            effectiveTimeIn = timeIn.Value;
            effectiveTimeOut = timeOut.Value;
            return true;
        }

        if (timeIn.HasValue && overtimeIn.HasValue && overtimeOut.HasValue)
        {
            var times = new[] { timeIn.Value, overtimeIn.Value, overtimeOut.Value };
            effectiveTimeIn = times.Min();
            effectiveTimeOut = times.Max();
            return true;
        }

        effectiveTimeIn = default;
        effectiveTimeOut = default;
        return false;
    }
}
