using FliqPayroll.Core.Enums;

namespace FliqPayroll.Data.Entities;

public class PayrollRecord
{
    public int Id { get; set; }
    public int PayrollPeriodId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string? Position { get; set; }
    public SalaryType SalaryType { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal MonthlySalary { get; set; }
    public decimal BiMonthlySalary { get; set; }
    public decimal DailyRate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal WorkingDays { get; set; }
    public decimal AbsentDays { get; set; }
    public decimal AbsentAmount { get; set; }
    public decimal BasicPayAmount { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal RegularOtRate { get; set; }
    public decimal RegularOtHours { get; set; }
    public decimal SpecialOtRate { get; set; }
    public decimal SpecialOtHours { get; set; }
    public decimal SpecialOtPay { get; set; }
    public decimal HolidayOtRate { get; set; }
    public decimal HolidayDays { get; set; }
    public decimal HolidayOtPay { get; set; }
    public decimal NightDiffOtRate { get; set; }
    public decimal NightDiffHours { get; set; }
    public decimal NightDiffOtPay { get; set; }
    public decimal LeaveDays { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal HolidayPay { get; set; }
    public decimal RegularHolidayPay { get; set; }
    public decimal SpecialNonWorkingPay { get; set; }
    public decimal LeaveWithPay { get; set; }
    public decimal Incentives { get; set; }
    public decimal Allowances { get; set; }
    public decimal Bonuses { get; set; }
    public decimal AdjustmentsEarnings { get; set; }
    public decimal GrossPay { get; set; }
    public decimal AbsenceDeduction { get; set; }
    public decimal LateDeduction { get; set; }
    public decimal LateUndertimeHours { get; set; }
    public decimal LateUndertimeAmount { get; set; }
    public decimal UndertimeDeduction { get; set; }
    public decimal CashAdvance { get; set; }
    public decimal SssDeduction { get; set; }
    public decimal PhilHealthDeduction { get; set; }
    public decimal PagIbigDeduction { get; set; }
    public decimal SssLoanDeduction { get; set; }
    public decimal SssCalamityDeduction { get; set; }
    public decimal PagIbigLoanDeduction { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal ToAdd { get; set; }
    public decimal ToDeduct { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string? PaymentMethod { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Calculated;
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
