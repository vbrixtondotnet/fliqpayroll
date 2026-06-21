namespace FliqPayroll.Web.Models;

public class PayrollV2PageViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
}

public class PayrollV2RowViewModel
{
    public string SalaryType { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal MonthlySalary { get; set; }
    public decimal BiMonthlySalary { get; set; }
    public decimal DailyRate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal WorkingDays { get; set; }
    public decimal AbsentDays { get; set; }
    public decimal AbsentAmount { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal RegularOtRate { get; set; }
    public decimal RegularOtHours { get; set; }
    public decimal RegularOtPay { get; set; }
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
    public decimal LeavePay { get; set; }
    public decimal Sss { get; set; }
    public decimal PhilHealth { get; set; }
    public decimal PagIbig { get; set; }
    public decimal LateUndertimeHours { get; set; }
    public decimal LateUndertimeAmount { get; set; }
    public decimal SssLoan { get; set; }
    public decimal SssCalamity { get; set; }
    public decimal PagIbigLoan { get; set; }
    public decimal ToAdd { get; set; }
    public decimal ToDeduct { get; set; }
    public decimal NetSalary { get; set; }
    public string PaymentMethod { get; set; } = "BPI";
}
