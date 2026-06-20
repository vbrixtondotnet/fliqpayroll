namespace FliqPayroll.Core.DTOs;

public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public decimal TotalMonthlyPayroll { get; set; }
    public int PendingPayrollCount { get; set; }
}
