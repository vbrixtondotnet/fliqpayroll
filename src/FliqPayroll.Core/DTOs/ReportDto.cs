namespace FliqPayroll.Core.DTOs;

public class PayslipDto
{
    public PayrollDto Payroll { get; set; } = new();
    public PayrollPeriodDto Period { get; set; } = new();
    public EmployeeDto Employee { get; set; } = new();
}

public class PayrollSummaryReportDto
{
    public int PayrollPeriodId { get; set; }
    public PayrollPeriodDto Period { get; set; } = new();
    public IReadOnlyList<PayrollDto> Records { get; set; } = [];
    public decimal TotalGrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
}

public class EmployeePayrollHistoryDto
{
    public EmployeeDto Employee { get; set; } = new();
    public IReadOnlyList<PayrollDto> History { get; set; } = [];
}
