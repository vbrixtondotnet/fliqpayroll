using FliqPayroll.Core.Enums;

namespace FliqPayroll.Core.DTOs;

public class LeaveDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateLeaveDto
{
    public int EmployeeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public LeaveType LeaveType { get; set; }
}
