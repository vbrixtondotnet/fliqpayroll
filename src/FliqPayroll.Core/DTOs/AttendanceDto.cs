namespace FliqPayroll.Core.DTOs;

public class AttendanceDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan? TimeIn { get; set; }
    public TimeSpan? TimeOut { get; set; }
    public bool IsLate { get; set; }
    public TimeSpan? OvertimeIn { get; set; }
    public TimeSpan? OvertimeOut { get; set; }
    public bool IsOvertimeValid { get; set; }
    public bool IsFromBiometric { get; set; }
    public string? Notes { get; set; }

    public bool IsAttendanceValid => TimeIn.HasValue && TimeOut.HasValue;

    public decimal HoursWorked =>
        IsAttendanceValid && TimeIn.HasValue && TimeOut.HasValue
            ? Math.Max(0m, Math.Round((decimal)(TimeOut.Value - TimeIn.Value).TotalHours, 2, MidpointRounding.AwayFromZero))
            : 0m;

    public decimal OvertimeHours =>
        IsOvertimeValid && OvertimeIn.HasValue && OvertimeOut.HasValue
            ? Math.Round((decimal)(OvertimeOut.Value - OvertimeIn.Value).TotalHours, 2, MidpointRounding.AwayFromZero)
            : 0m;
}

public class UpdateAttendanceDto
{
    public int Id { get; set; }
    public TimeSpan? TimeIn { get; set; }
    public TimeSpan? TimeOut { get; set; }
    public bool IsLate { get; set; }
    public TimeSpan? OvertimeIn { get; set; }
    public TimeSpan? OvertimeOut { get; set; }
    public bool IsOvertimeValid { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceUploadResultDto
{
    public int UploadId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ProcessedDays { get; set; }
    public int SkippedIncomplete { get; set; }
    public int UnmatchedRows { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
}

public class AttendanceSummaryDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEmployees { get; set; }
    public int ValidAttendanceDays { get; set; }
    public int LateDays { get; set; }
    public int IncompleteDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
}

public class BiometricCsvPunchDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int AttendanceCode { get; set; }
}

public class ProcessedAttendanceDayDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan? TimeIn { get; set; }
    public TimeSpan? TimeOut { get; set; }
    public bool IsLate { get; set; }
    public TimeSpan? OvertimeIn { get; set; }
    public TimeSpan? OvertimeOut { get; set; }
    public bool IsOvertimeValid { get; set; }
    public bool IsAttendanceValid { get; set; }
}
