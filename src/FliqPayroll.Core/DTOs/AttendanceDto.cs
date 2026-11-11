namespace FliqPayroll.Core.DTOs;

using FliqPayroll.Core.Constants;
using FliqPayroll.Core.Utilities;
using System.Text.Json.Serialization;

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

    /// <summary>Name of the employee's assigned shift (the default Day Shift when none is assigned).</summary>
    public string ShiftName { get; set; } = ShiftConstants.DefaultShiftName;
    public TimeSpan ShiftStart { get; set; } = AttendanceConstants.WorkStart;
    public TimeSpan ShiftEnd { get; set; } = AttendanceConstants.WorkEnd;

    [JsonIgnore]
    public ShiftSchedule Shift => new(ShiftName, ShiftStart, ShiftEnd);

    /// <summary>
    /// Present only when regular Time In and Time Out exist.
    /// </summary>
    public bool IsAttendanceValid =>
        AttendancePolicyProcessor.IsPresent(TimeIn, TimeOut);

    public decimal HoursWorked =>
        AttendancePolicyProcessor.CalculateHoursWorked(TimeIn, TimeOut);

    public decimal OvertimeHours =>
        AttendancePolicyProcessor.CalculateOvertimeHours(TimeIn, TimeOut, Shift);

    public decimal NightDifferentialHours =>
        AttendancePolicyProcessor.CalculateNightDifferentialHours(TimeIn, TimeOut);

    public decimal LateMinutes =>
        AttendancePolicyProcessor.TryGetEffectiveTimeWindow(
            TimeIn, TimeOut, out var effectiveIn, out _)
            ? AttendanceConstants.CalculateLateMinutes(effectiveIn, Shift)
            : AttendanceConstants.CalculateLateMinutes(TimeIn, Shift);

    /// <summary>Minutes short of the shift end (Time Out before the scheduled end).</summary>
    public decimal UndertimeMinutes => Shift.CalculateUndertimeMinutes(TimeIn, TimeOut);
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
