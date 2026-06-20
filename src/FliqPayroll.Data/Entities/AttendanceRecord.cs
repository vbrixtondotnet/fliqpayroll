namespace FliqPayroll.Data.Entities;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan? TimeIn { get; set; }
    public TimeSpan? TimeOut { get; set; }
    public bool IsLate { get; set; }
    public TimeSpan? OvertimeIn { get; set; }
    public TimeSpan? OvertimeOut { get; set; }
    public bool IsOvertimeValid { get; set; }
    public bool IsFromBiometric { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
}
