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

        var hasTimeIn = punches.Any(p => p.AttendanceCode == AttendanceConstants.CodeTimeIn);
        var hasTimeOut = punches.Any(p => p.AttendanceCode == AttendanceConstants.CodeTimeOut);
        var isAttendanceValid = hasTimeIn && hasTimeOut;

        if (!isAttendanceValid)
        {
            return null;
        }

        var isLate = AttendanceConstants.IsLateTimeIn(timeIn);

        var hasOtIn = punches.Any(p => p.AttendanceCode == AttendanceConstants.CodeOvertimeIn);
        var hasOtOut = punches.Any(p => p.AttendanceCode == AttendanceConstants.CodeOvertimeOut);
        var isOvertimeValid = hasOtIn && hasOtOut;

        return new ProcessedAttendanceDayDto
        {
            EmployeeId = employeeId,
            EmployeeCode = employeeCode,
            Date = PhilippineTime.ForDateStorage(date),
            TimeIn = timeIn,
            TimeOut = timeOut,
            IsLate = isLate,
            OvertimeIn = isOvertimeValid ? overtimeIn : null,
            OvertimeOut = isOvertimeValid ? overtimeOut : null,
            IsOvertimeValid = isOvertimeValid,
            IsAttendanceValid = true
        };
    }
}
