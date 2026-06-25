using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Enums;

namespace FliqPayroll.Core.Utilities;

public static class PayrollHolidayPayCalculator
{
    public sealed record Result(
        decimal HolidayPay,
        decimal RegularHolidayPay,
        decimal SpecialHolidayPay,
        decimal SpecialOtHours,
        decimal WorkingDays,
        decimal AbsentDays,
        decimal RegularHolidayDays,
        bool RegularHolidayWorked,
        int RegularHolidayAbsentCount,
        decimal SpecialHolidayDays,
        decimal LeaveDays);

    public static Result Calculate(
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyList<AttendanceDto> attendance,
        IReadOnlyDictionary<DateTime, HolidayDto> holidaysByDate,
        decimal dailyRate,
        IReadOnlyList<LeaveDto> leaves)
    {
        leaves ??= Array.Empty<LeaveDto>();

        var start = PhilippineTime.ToPhilippineDate(periodStart);
        var end = PhilippineTime.ToPhilippineDate(periodEnd);

        if (end < start)
        {
            return new Result(0m, 0m, 0m, 0m, 0m, 0m, 0m, false, 0, 0m, 0m);
        }

        var leaveDates = BuildLeaveDates(leaves, start, end);

        var attendanceByDate = attendance
            .GroupBy(a => PhilippineTime.ToPhilippineDate(a.Date))
            .ToDictionary(g => g.Key, g => g.First());

        decimal workingDays = 0;
        decimal absentDays = 0;
        decimal regularHolidayPay = 0;
        decimal specialHolidayPay = 0;
        decimal specialOtHours = 0;
        decimal regularHolidayDays = 0;
        decimal specialHolidayDays = 0;
        decimal leaveDays = 0;
        var regularHolidayWorked = false;
        var regularHolidayAbsentCount = 0;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (PhilippineTime.IsSunday(date))
            {
                attendanceByDate.TryGetValue(date, out var sundayRecord);
                if (sundayRecord?.IsAttendanceValid == true)
                {
                    var sundayHours = sundayRecord.HoursWorked;
                    specialOtHours += sundayHours;
                    specialHolidayPay += CalculateHoursBasedHolidayPay(
                        dailyRate,
                        sundayHours,
                        PayrollConstants.SpecialNonWorkingRate);
                }

                continue;
            }

            attendanceByDate.TryGetValue(date, out var record);
            var isPresent = record?.IsAttendanceValid == true;
            var hoursWorked = record?.HoursWorked ?? 0m;

            if (holidaysByDate.TryGetValue(date, out var holiday))
            {
                switch (holiday.HolidayType)
                {
                    case HolidayType.Regular:
                        if (isPresent)
                        {
                            regularHolidayWorked = true;
                            regularHolidayDays += HoursToDayEquivalent(hoursWorked);
                            regularHolidayPay += CalculateHoursBasedHolidayPay(
                                dailyRate,
                                hoursWorked,
                                PayrollConstants.RegularHolidayPresentMultiplier);
                        }
                        else
                        {
                            regularHolidayAbsentCount++;
                            regularHolidayDays += 1m;
                            regularHolidayPay += dailyRate * PayrollConstants.RegularHolidayRate;
                        }

                        break;

                    case HolidayType.Special:
                        specialHolidayDays++;
                        if (isPresent)
                        {
                            specialOtHours += hoursWorked;
                            specialHolidayPay += CalculateHoursBasedHolidayPay(
                                dailyRate,
                                hoursWorked,
                                PayrollConstants.SpecialNonWorkingRate);
                        }

                        break;
                }

                continue;
            }

            if (isPresent)
            {
                workingDays++;
            }
            else if (leaveDates.Contains(date))
            {
                leaveDays++;
            }
            else
            {
                absentDays++;
            }
        }

        regularHolidayPay = Round(regularHolidayPay);
        specialHolidayPay = Round(specialHolidayPay);
        specialOtHours = Round(specialOtHours);
        regularHolidayDays = Round(regularHolidayDays);

        return new Result(
            regularHolidayPay + specialHolidayPay,
            regularHolidayPay,
            specialHolidayPay,
            specialOtHours,
            workingDays,
            absentDays,
            regularHolidayDays,
            regularHolidayWorked,
            regularHolidayAbsentCount,
            specialHolidayDays,
            leaveDays);
    }

    private static HashSet<DateTime> BuildLeaveDates(
        IReadOnlyList<LeaveDto> leaves,
        DateTime periodStart,
        DateTime periodEnd)
    {
        var leaveDates = new HashSet<DateTime>();

        foreach (var leave in leaves)
        {
            var leaveStart = PhilippineTime.ToPhilippineDate(leave.FromDate);
            var leaveEnd = PhilippineTime.ToPhilippineDate(leave.ToDate);
            var rangeStart = leaveStart < periodStart ? periodStart : leaveStart;
            var rangeEnd = leaveEnd > periodEnd ? periodEnd : leaveEnd;

            if (rangeEnd < rangeStart)
            {
                continue;
            }

            for (var date = rangeStart; date <= rangeEnd; date = date.AddDays(1))
            {
                if (!PhilippineTime.IsSunday(date))
                {
                    leaveDates.Add(date);
                }
            }
        }

        return leaveDates;
    }

    public static decimal CalculateSpecialNonWorkingPay(decimal dailyRate, decimal hoursWorked) =>
        CalculateHoursBasedHolidayPay(dailyRate, hoursWorked, PayrollConstants.SpecialNonWorkingRate);

    public static decimal CalculateRegularHolidayPresentPay(decimal dailyRate, decimal hoursWorked) =>
        CalculateHoursBasedHolidayPay(dailyRate, hoursWorked, PayrollConstants.RegularHolidayPresentMultiplier);

    public static decimal CalculateHoursBasedHolidayPay(decimal dailyRate, decimal hoursWorked, decimal rate) =>
        hoursWorked >= PayrollConstants.HoursPerDay
            ? dailyRate * rate
            : ((dailyRate * rate) / PayrollConstants.HoursPerDay) * hoursWorked;

    private static decimal HoursToDayEquivalent(decimal hoursWorked) =>
        Round(hoursWorked / PayrollConstants.HoursPerDay);

    public static IReadOnlyDictionary<DateTime, HolidayDto> ToHolidayMap(IReadOnlyList<HolidayDto> holidays) =>
        holidays.ToDictionary(h => PhilippineTime.ToPhilippineDate(h.Date));

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
