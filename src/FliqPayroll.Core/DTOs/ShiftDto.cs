namespace FliqPayroll.Core.DTOs;

public class ShiftEmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public int? ShiftId { get; set; }
}

public class ShiftDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>24-hour "HH:mm".</summary>
    public string StartTime { get; set; } = string.Empty;
    /// <summary>24-hour "HH:mm".</summary>
    public string EndTime { get; set; } = string.Empty;
    public List<ShiftEmployeeDto> Employees { get; set; } = new();
}

public class UpdateShiftDto
{
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

public class AssignShiftEmployeesDto
{
    /// <summary>Full set of employee ids that should belong to the shift after saving.</summary>
    public List<int> EmployeeIds { get; set; } = new();
}
