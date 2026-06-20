using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Enums;

namespace FliqPayroll.Core.Utilities;

public static class PayrollPeriodHelper
{
    public static PayrollPeriodDto CreateRange(DateTime fromDate, DateTime toDate)
    {
        var start = PhilippineTime.ToPhilippineDate(fromDate);
        var end = PhilippineTime.ToPhilippineDate(toDate);

        if (end < start)
        {
            throw new ArgumentException("End date must be on or after start date.", nameof(toDate));
        }

        return new PayrollPeriodDto
        {
            Name = $"{start:MMM d} - {end:MMM d, yyyy}",
            StartDate = start,
            EndDate = end,
            CutoffDay = end.Day,
            Status = PayrollPeriodStatus.Open
        };
    }

    public static PayrollPeriodDto ResolveDefaultPeriod(DateTime referenceDate)
    {
        var phReference = PhilippineTime.ToPhilippineDate(referenceDate);
        var (startDate, endDate, cutoffDay, name) = ResolvePeriod(phReference);

        return new PayrollPeriodDto
        {
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            CutoffDay = cutoffDay,
            Status = PayrollPeriodStatus.Open
        };
    }

    private static (DateTime StartDate, DateTime EndDate, int CutoffDay, string Name) ResolvePeriod(DateTime referenceDate)
    {
        var year = referenceDate.Year;
        var month = referenceDate.Month;
        var day = referenceDate.Day;

        DateTime startDate;
        DateTime endDate;
        int cutoffDay;

        if (day <= PayrollConstants.DefaultFirstCutoffDay)
        {
            startDate = new DateTime(year, month, 1);
            endDate = new DateTime(year, month, PayrollConstants.DefaultFirstCutoffDay);
            cutoffDay = PayrollConstants.DefaultFirstCutoffDay;
        }
        else if (day <= PayrollConstants.DefaultSecondCutoffDay)
        {
            startDate = new DateTime(year, month, PayrollConstants.DefaultFirstCutoffDay + 1);
            endDate = new DateTime(year, month, PayrollConstants.DefaultSecondCutoffDay);
            cutoffDay = PayrollConstants.DefaultSecondCutoffDay;
        }
        else
        {
            startDate = new DateTime(year, month, PayrollConstants.DefaultSecondCutoffDay + 1);
            endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            cutoffDay = PayrollConstants.DefaultSecondCutoffDay;
        }

        var name = $"{startDate:MMM d} - {endDate:MMM d, yyyy}";
        return (startDate, endDate, cutoffDay, name);
    }
}
