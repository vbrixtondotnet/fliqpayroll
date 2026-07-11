namespace FliqPayroll.Core.DTOs;

using FliqPayroll.Core.Constants;
using FliqPayroll.Core.Utilities;

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

    /// <summary>
    /// Present when Time In + Time Out exist, or Time In + complete Overtime In/Out (OT Out alone is ignored).
    /// </summary>
    public bool IsAttendanceValid =>
        AttendancePolicyProcessor.IsPresent(TimeIn, TimeOut, OvertimeIn, OvertimeOut);

    public decimal HoursWorked =>
        AttendancePolicyProcessor.TryGetEffectiveTimeWindow(
            TimeIn, TimeOut, OvertimeIn, OvertimeOut, out var effectiveIn, out var effectiveOut)
            ? Math.Max(0m, Math.Round((decimal)(effectiveOut - effectiveIn).TotalHours, 2, MidpointRounding.AwayFromZero))
            : 0m;

    public decimal OvertimeHours =>
        AttendancePolicyProcessor.HasValidOvertime(OvertimeIn, OvertimeOut)
            ? Math.Round((decimal)(OvertimeOut!.Value - OvertimeIn!.Value).TotalHours, 2, MidpointRounding.AwayFromZero)
            : 0m;

    public decimal LateMinutes =>
        AttendancePolicyProcessor.TryGetEffectiveTimeWindow(
            TimeIn, TimeOut, OvertimeIn, OvertimeOut, out var effectiveIn, out _)
            ? AttendanceConstants.CalculateLateMinutes(effectiveIn)
            : AttendanceConstants.CalculateLateMinutes(TimeIn);
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
