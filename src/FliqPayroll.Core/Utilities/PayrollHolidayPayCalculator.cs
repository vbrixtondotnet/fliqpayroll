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
        decimal WorkingDays,
        decimal AbsentDays,
        decimal RegularHolidayDays,
        decimal SpecialHolidayDays);

    public static Result Calculate(
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyList<AttendanceDto> attendance,
        IReadOnlyDictionary<DateTime, HolidayDto> holidaysByDate,
        decimal dailyRate)
    {
        var start = PhilippineTime.ToPhilippineDate(periodStart);
        var end = PhilippineTime.ToPhilippineDate(periodEnd);

        if (end < start)
        {
            return new Result(0m, 0m, 0m, 0m, 0m, 0m, 0m);
        }

        var attendanceByDate = attendance
            .GroupBy(a => PhilippineTime.ToPhilippineDate(a.Date))
            .ToDictionary(g => g.Key, g => g.First());

        decimal workingDays = 0;
        decimal absentDays = 0;
        decimal regularHolidayPay = 0;
        decimal specialHolidayPay = 0;
        decimal regularHolidayDays = 0;
        decimal specialHolidayDays = 0;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (PhilippineTime.IsSunday(date))
            {
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
                        regularHolidayDays++;
                        if (isPresent && hoursWorked >= PayrollConstants.HoursPerDay)
                        {
                            regularHolidayPay += dailyRate * PayrollConstants.RegularHolidayPresentMultiplier;
                        }
                        else if (!isPresent)
                        {
                            regularHolidayPay += dailyRate;
                        }

                        break;

                    case HolidayType.Special:
                        specialHolidayDays++;
                        if (isPresent)
                        {
                            specialHolidayPay += dailyRate * PayrollConstants.SpecialNonWorkingRate;
                        }

                        break;
                }

                continue;
            }

            if (isPresent)
            {
                workingDays++;
            }
            else
            {
                absentDays++;
            }
        }

        regularHolidayPay = Round(regularHolidayPay);
        specialHolidayPay = Round(specialHolidayPay);

        return new Result(
            regularHolidayPay + specialHolidayPay,
            regularHolidayPay,
            specialHolidayPay,
            workingDays,
            absentDays,
            regularHolidayDays,
            specialHolidayDays);
    }

    public static IReadOnlyDictionary<DateTime, HolidayDto> ToHolidayMap(IReadOnlyList<HolidayDto> holidays) =>
        holidays.ToDictionary(h => PhilippineTime.ToPhilippineDate(h.Date));

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
