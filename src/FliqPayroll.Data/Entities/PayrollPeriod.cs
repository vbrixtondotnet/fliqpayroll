using FliqPayroll.Core.Enums;

namespace FliqPayroll.Data.Entities;

public class PayrollPeriod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CutoffDay { get; set; }
    public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Locked;
    public bool IsLocked { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// Set when Finance Officers were emailed about pending changes. While set, further edits do not trigger another
    /// email; it is cleared once the period has no pending changes left.
    /// </summary>
    public DateTime? ApprovalNotifiedAt { get; set; }

    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
}
