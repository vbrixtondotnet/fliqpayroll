using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Enums;

namespace FliqPayroll.Core.Utilities;

public static class PayrollCalculator
{
    public static (decimal BiMonthly, decimal Daily, decimal Hourly) GetRates(SalaryType salaryType, decimal basicSalary)
    {
        return salaryType switch
        {
            SalaryType.Daily => (
                BiMonthly: basicSalary * PayrollConstants.WorkingDaysPerCutoff,
                Daily: basicSalary,
                Hourly: basicSalary / PayrollConstants.HoursPerDay),
            SalaryType.Fixed => (
                BiMonthly: basicSalary / 2m,
                Daily: (basicSalary / 2m) / PayrollConstants.WorkingDaysPerCutoff,
                Hourly: ((basicSalary / 2m) / PayrollConstants.WorkingDaysPerCutoff) / PayrollConstants.HoursPerDay),
            SalaryType.Monthly => (
                BiMonthly: basicSalary / 2m,
                Daily: basicSalary / PayrollConstants.MonthlyWorkingDaysDivisor,
                Hourly: (basicSalary / PayrollConstants.MonthlyWorkingDaysDivisor) / PayrollConstants.HoursPerDay),
            _ => (
                BiMonthly: basicSalary / 2m,
                Daily: basicSalary / PayrollConstants.MonthlyWorkingDaysDivisor,
                Hourly: (basicSalary / PayrollConstants.MonthlyWorkingDaysDivisor) / PayrollConstants.HoursPerDay)
        };
    }

    public static PayrollDto Compute(
        EmployeeDto employee,
        IReadOnlyList<AttendanceDto> attendance,
        PayrollPeriodDto period,
        IReadOnlyList<HolidayDto> holidays)
    {
        var holidayMap = PayrollHolidayPayCalculator.ToHolidayMap(holidays);

        return employee.SalaryType switch
        {
            SalaryType.Fixed => ComputeFixed(employee, attendance, period, holidayMap),
            SalaryType.Monthly => ComputeMonthly(employee, attendance, period, holidayMap),
            _ => ComputeDaily(employee, attendance, period, holidayMap)
        };
    }

    public static PayrollDto ComputeMonthly(
        EmployeeDto employee,
        IReadOnlyList<AttendanceDto> attendance,
        PayrollPeriodDto period,
        IReadOnlyDictionary<DateTime, HolidayDto> holidaysByDate)
    {
        var rates = GetRates(SalaryType.Monthly, employee.BasicSalary);
        var biMonthlyPay = Round(rates.BiMonthly);
        var dailyRate = Round(rates.Daily);

        var metrics = PayrollHolidayPayCalculator.Calculate(
            period.StartDate,
            period.EndDate,
            attendance,
            holidaysByDate,
            dailyRate);

        var absenceDeduction = Round(dailyRate * metrics.AbsentDays);
        var grossPay = Round(biMonthlyPay + metrics.HolidayPay);
        var netPay = Round(biMonthlyPay - absenceDeduction + metrics.HolidayPay);

        return BuildPayrollDto(employee, SalaryType.Monthly, rates, metrics, biMonthlyPay, 0m, grossPay, absenceDeduction, netPay);
    }

    public static PayrollDto ComputeFixed(
        EmployeeDto employee,
        IReadOnlyList<AttendanceDto> attendance,
        PayrollPeriodDto period,
        IReadOnlyDictionary<DateTime, HolidayDto> holidaysByDate)
    {
        var rates = GetRates(SalaryType.Fixed, employee.BasicSalary);
        var basePay = Round(employee.BasicSalary / 2m);
        var dailyRate = Round(rates.Daily);

        var metrics = PayrollHolidayPayCalculator.Calculate(
            period.StartDate,
            period.EndDate,
            attendance,
            holidaysByDate,
            dailyRate);

        var grossPay = Round(basePay + metrics.HolidayPay);
        var netPay = grossPay;

        return BuildPayrollDto(employee, SalaryType.Fixed, rates, metrics, basePay, 0m, grossPay, 0m, netPay);
    }

    private static PayrollDto ComputeDaily(
        EmployeeDto employee,
        IReadOnlyList<AttendanceDto> attendance,
        PayrollPeriodDto period,
        IReadOnlyDictionary<DateTime, HolidayDto> holidaysByDate)
    {
        var rates = GetRates(SalaryType.Daily, employee.BasicSalary);
        var dailyRate = Round(rates.Daily);

        var metrics = PayrollHolidayPayCalculator.Calculate(
            period.StartDate,
            period.EndDate,
            attendance,
            holidaysByDate,
            dailyRate);

        var payableAttendance = attendance.Where(a =>
            a.IsAttendanceValid && !PhilippineTime.IsSunday(a.Date));

        var basicPay = Round(dailyRate * metrics.WorkingDays);
        var overtimePay = Round(
            payableAttendance.Sum(a => a.OvertimeHours) * rates.Hourly * PayrollConstants.RegularOvertimeRate);
        var grossPay = Round(basicPay + metrics.HolidayPay + overtimePay);
        var netPay = grossPay;

        return BuildPayrollDto(employee, SalaryType.Daily, rates, metrics, basicPay, overtimePay, grossPay, 0m, netPay);
    }

    private static PayrollDto BuildPayrollDto(
        EmployeeDto employee,
        SalaryType salaryType,
        (decimal BiMonthly, decimal Daily, decimal Hourly) rates,
        PayrollHolidayPayCalculator.Result metrics,
        decimal basicPayAmount,
        decimal overtimePay,
        decimal grossPay,
        decimal absenceDeduction,
        decimal netPay) => new()
    {
        EmployeeId = employee.Id,
        EmployeeName = employee.FullName,
        EmployeeCode = employee.EmployeeCode,
        Position = employee.Position,
        SalaryType = salaryType,
        BasicSalary = employee.BasicSalary,
        BiMonthlySalary = rates.BiMonthly,
        DailyRate = Round(rates.Daily),
        HourlyRate = rates.Hourly,
        WorkingDays = metrics.WorkingDays,
        AbsentDays = metrics.AbsentDays,
        BasicPayAmount = basicPayAmount,
        OvertimePay = overtimePay,
        HolidayPay = metrics.HolidayPay,
        RegularHolidayPay = metrics.RegularHolidayPay,
        SpecialNonWorkingPay = metrics.SpecialHolidayPay,
        LeaveWithPay = 0m,
        Incentives = 0m,
        Allowances = 0m,
        Bonuses = 0m,
        AdjustmentsEarnings = 0m,
        GrossPay = grossPay,
        AbsenceDeduction = absenceDeduction,
        LateDeduction = 0m,
        UndertimeDeduction = 0m,
        CashAdvance = 0m,
        SssDeduction = 0m,
        PhilHealthDeduction = 0m,
        PagIbigDeduction = 0m,
        SssLoanDeduction = 0m,
        PagIbigLoanDeduction = 0m,
        WithholdingTax = 0m,
        OtherDeductions = 0m,
        TotalDeductions = absenceDeduction,
        NetPay = netPay,
        Status = PayrollStatus.Calculated
    };

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
