using FliqPayroll.Core.Enums;

namespace FliqPayroll.Data.Entities;

public class LeaveRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
}
