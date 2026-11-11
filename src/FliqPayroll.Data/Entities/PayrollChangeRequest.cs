using FliqPayroll.Core.Enums;

namespace FliqPayroll.Data.Entities;

/// <summary>
/// A manual edit to an already-generated payroll row. Stays <see cref="PayrollChangeStatus.Pending"/> until an
/// approver reviews it. Rows are never deleted, so the table doubles as the payroll audit trail.
/// </summary>
public class PayrollChangeRequest
{
    public int Id { get; set; }
    public int PayrollPeriodId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public PayrollChangeStatus Status { get; set; } = PayrollChangeStatus.Pending;
    /// <summary>JSON-serialised <c>PayrollDto</c> the user wants the live record to become.</summary>
    public string ProposedJson { get; set; } = string.Empty;
    /// <summary>JSON array of field-level differences (old vs new) captured at submission time.</summary>
    public string ChangesJson { get; set; } = "[]";
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public PayrollPeriod PayrollPeriod { get; set; } = null!;
}
