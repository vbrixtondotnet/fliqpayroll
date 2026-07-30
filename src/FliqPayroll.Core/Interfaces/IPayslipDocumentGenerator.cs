using FliqPayroll.Core.DTOs;

namespace FliqPayroll.Core.Interfaces;

public interface IPayslipDocumentGenerator
{
    /// <summary>
    /// Generates the Employee Copy only (acknowledgment section included).
    /// Used when emailing a payslip to the employee.
    /// </summary>
    byte[] GenerateEmployeeCopy(PayslipDto payslip);

    /// <summary>
    /// Dual-copy layout (Employee + Company) used for PDF downloads.
    /// </summary>
    byte[] Generate(PayslipDto payslip);

    string BuildSinglePayslipFileName(PayslipDto payslip);

    string FormatPayrollPeriod(DateTime startDate, DateTime endDate);
}
