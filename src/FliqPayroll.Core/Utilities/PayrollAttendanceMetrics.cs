using FliqPayroll.Core.DTOs;

namespace FliqPayroll.Core.Utilities;

public static class PayrollAttendanceMetrics
{
    public static (decimal WorkingDays, decimal AbsentDays) Calculate(
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyList<AttendanceDto> attendance)
    {
        var start = PhilippineTime.ToPhilippineDate(periodStart);
        var end = PhilippineTime.ToPhilippineDate(periodEnd);

        if (end < start)
        {
            return (0m, 0m);
        }

        var validAttendanceDates = attendance
            .Where(a => a.IsAttendanceValid && !PhilippineTime.IsSunday(a.Date))
            .Select(a => PhilippineTime.ToPhilippineDate(a.Date))
            .ToHashSet();

        var workingDays = validAttendanceDates.Count;
        var absentDays = 0;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (PhilippineTime.IsSunday(date))
            {
                continue;
            }

            if (!validAttendanceDates.Contains(date))
            {
                absentDays++;
            }
        }

        return (workingDays, absentDays);
    }
}
