using FliqPayroll.Core.Enums;

namespace FliqPayroll.Core.DTOs;

public class PayrollFieldChangeDto
{
    /// <summary>Property name on the payroll record (e.g. "SssDeduction").</summary>
    public string Field { get; set; } = string.Empty;
    /// <summary>Human readable label (e.g. "Sss Deduction").</summary>
    public string Label { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class PayrollChangeRequestDto
{
    public int Id { get; set; }
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public PayrollChangeStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public IReadOnlyList<PayrollFieldChangeDto> Changes { get; set; } = [];
    /// <summary>The proposed payroll row; only populated when explicitly requested.</summary>
    public PayrollDto? ProposedRecord { get; set; }
}

public class ReviewPayrollChangeRequestDto
{
    public string? Note { get; set; }
}

/// <summary>One payroll period that has edits waiting for a Finance Officer.</summary>
public class PayrollApprovalSummaryDto
{
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    /// <summary>Employees (payroll rows) with an unreviewed edit.</summary>
    public int PendingEmployeeCount { get; set; }
    /// <summary>Total number of individual fields changed across those rows.</summary>
    public int PendingChangeCount { get; set; }
    public DateTime LatestRequestedAt { get; set; }
    public IReadOnlyList<string> RequestedBy { get; set; } = [];
}

public class BulkReviewResultDto
{
    public int ReviewedCount { get; set; }
}
