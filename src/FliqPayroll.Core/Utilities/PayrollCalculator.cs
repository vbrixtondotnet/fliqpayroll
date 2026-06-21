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

        return BuildPayrollDto(
            employee,
            SalaryType.Monthly,
            rates,
            metrics,
            attendance,
            biMonthlyPay,
            0m,
            grossPay,
            absenceDeduction);
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

        return BuildPayrollDto(
            employee,
            SalaryType.Fixed,
            rates,
            metrics,
            attendance,
            basePay,
            0m,
            grossPay,
            0m);
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

        var payableAttendance = GetPayableAttendance(attendance);

        var basicPay = Round(dailyRate * metrics.WorkingDays);
        var overtimePay = Round(
            payableAttendance.Sum(a => a.OvertimeHours) * rates.Hourly * PayrollConstants.RegularOvertimeRate);
        var grossPay = Round(basicPay + metrics.HolidayPay + overtimePay);

        return BuildPayrollDto(
            employee,
            SalaryType.Daily,
            rates,
            metrics,
            attendance,
            basicPay,
            overtimePay,
            grossPay,
            0m);
    }

    private static PayrollDto BuildPayrollDto(
        EmployeeDto employee,
        SalaryType salaryType,
        (decimal BiMonthly, decimal Daily, decimal Hourly) rates,
        PayrollHolidayPayCalculator.Result metrics,
        IReadOnlyList<AttendanceDto> attendance,
        decimal basicPayAmount,
        decimal overtimePay,
        decimal grossPay,
        decimal absenceDeduction)
    {
        var payableAttendance = GetPayableAttendance(attendance);
        var regularOtHours = Round(payableAttendance.Sum(a => a.OvertimeHours));
        var lateUndertimeHours = Round(attendance.Count(a => a.IsAttendanceValid && a.IsLate));
        var lateUndertimeAmount = Round(lateUndertimeHours * rates.Hourly);

        var monthlySalary = salaryType switch
        {
            SalaryType.Daily => 0m,
            _ => employee.BasicSalary
        };

        var biMonthlySalary = salaryType == SalaryType.Daily ? 0m : Round(rates.BiMonthly);

        var absentAmount = salaryType == SalaryType.Monthly
            ? absenceDeduction
            : Round(rates.Daily * metrics.AbsentDays);

        var sssDeduction = employee.SssEeShare;
        var philHealthDeduction = employee.PhilHealthEeShare;
        var pagIbigDeduction = employee.PagIbigEeShare;
        var sssLoanDeduction = employee.SssLoan;
        var pagIbigLoanDeduction = employee.PagIbigLoan;
        var sssCalamityDeduction = 0m;

        var totalDeductions = Round(
            absenceDeduction +
            sssDeduction +
            philHealthDeduction +
            pagIbigDeduction +
            sssLoanDeduction +
            pagIbigLoanDeduction +
            sssCalamityDeduction +
            lateUndertimeAmount);

        var netPay = Round(grossPay - sssDeduction - philHealthDeduction - pagIbigDeduction - sssLoanDeduction - pagIbigLoanDeduction - sssCalamityDeduction - lateUndertimeAmount);
        if (salaryType == SalaryType.Monthly)
        {
            netPay = Round(grossPay - absenceDeduction - sssDeduction - philHealthDeduction - pagIbigDeduction - sssLoanDeduction - pagIbigLoanDeduction - sssCalamityDeduction - lateUndertimeAmount);
        }

        return new PayrollDto
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            EmployeeCode = employee.EmployeeCode,
            Position = employee.Position,
            SalaryType = salaryType,
            BasicSalary = employee.BasicSalary,
            MonthlySalary = monthlySalary,
            BiMonthlySalary = biMonthlySalary,
            DailyRate = Round(rates.Daily),
            HourlyRate = Round(rates.Hourly),
            WorkingDays = metrics.WorkingDays,
            AbsentDays = metrics.AbsentDays,
            AbsentAmount = absentAmount,
            BasicPayAmount = basicPayAmount,
            GrossSalary = basicPayAmount,
            RegularOtRate = PayrollConstants.RegularOvertimeRate,
            RegularOtHours = regularOtHours,
            OvertimePay = overtimePay,
            SpecialOtRate = PayrollConstants.SpecialNonWorkingRate,
            SpecialOtHours = 0m,
            SpecialOtPay = metrics.SpecialHolidayPay,
            HolidayOtRate = PayrollConstants.RegularHolidayRate,
            HolidayDays = metrics.RegularHolidayDays,
            HolidayOtPay = metrics.RegularHolidayPay,
            NightDiffOtRate = 0m,
            NightDiffHours = 0m,
            NightDiffOtPay = 0m,
            LeaveDays = 0m,
            HolidayPay = metrics.HolidayPay,
            RegularHolidayPay = metrics.RegularHolidayPay,
            SpecialNonWorkingPay = metrics.SpecialHolidayPay,
            LeaveWithPay = 0m,
            Incentives = 0m,
            Allowances = 0m,
            Bonuses = 0m,
            AdjustmentsEarnings = 0m,
            ToAdd = 0m,
            ToDeduct = 0m,
            GrossPay = grossPay,
            AbsenceDeduction = absenceDeduction,
            LateDeduction = lateUndertimeAmount,
            LateUndertimeHours = lateUndertimeHours,
            LateUndertimeAmount = lateUndertimeAmount,
            UndertimeDeduction = 0m,
            CashAdvance = 0m,
            SssDeduction = sssDeduction,
            PhilHealthDeduction = philHealthDeduction,
            PagIbigDeduction = pagIbigDeduction,
            SssLoanDeduction = sssLoanDeduction,
            SssCalamityDeduction = sssCalamityDeduction,
            PagIbigLoanDeduction = pagIbigLoanDeduction,
            WithholdingTax = 0m,
            OtherDeductions = 0m,
            TotalDeductions = totalDeductions,
            NetPay = netPay,
            PaymentMethod = string.IsNullOrWhiteSpace(employee.BankName) ? "Cash" : employee.BankName,
            Status = PayrollStatus.Calculated
        };
    }

    private static IEnumerable<AttendanceDto> GetPayableAttendance(IReadOnlyList<AttendanceDto> attendance) =>
        attendance.Where(a => a.IsAttendanceValid && !PhilippineTime.IsSunday(a.Date));

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
