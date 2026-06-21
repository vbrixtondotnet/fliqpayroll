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

    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
}
