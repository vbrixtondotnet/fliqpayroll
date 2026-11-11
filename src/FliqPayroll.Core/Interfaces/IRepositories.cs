using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;



namespace FliqPayroll.Core.Interfaces;



public interface IEmployeeRepository

{

    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(EmployeeFilterDto? filter = null, CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, int>> GetEmployeeIdsByCodesAsync(CancellationToken cancellationToken = default);

    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default);

    Task<EmployeeDto?> UpdateAsync(UpdateEmployeeDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

}



public interface IAttendanceRepository

{

    Task<IReadOnlyList<AttendanceDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceDto>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);

    Task<AttendanceDto?> UpdateAsync(UpdateAttendanceDto dto, CancellationToken cancellationToken = default);

    Task<int> CountValidByDateAsync(DateTime date, CancellationToken cancellationToken = default);

    Task<int> CountLateByDateAsync(DateTime date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceDto>> GetByEmployeeAndPeriodAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task UpsertBiometricAsync(ProcessedAttendanceDayDto record, CancellationToken cancellationToken = default);
    /// <summary>Shift schedule per employee id; employees without a shift get the default Day Shift.</summary>
    Task<IReadOnlyDictionary<int, ShiftSchedule>> GetEmployeeSchedulesAsync(CancellationToken cancellationToken = default);
    /// <summary>Schedule of the employee owning the attendance record (default Day Shift when unassigned).</summary>
    Task<ShiftSchedule> GetScheduleForAttendanceAsync(int attendanceId, CancellationToken cancellationToken = default);

    Task<int> SaveUploadLogAsync(string fileName, int totalRows, int processedDays, int skippedIncomplete, int unmatchedRows, CancellationToken cancellationToken = default);

}



public interface IAuditRepository
{
    Task LogAsync(string userName, string action, string entityName, string? entityId, string? details, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default);
}

public interface IHolidayRepository
{
    Task<IReadOnlyList<HolidayDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HolidayDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<HolidayDto?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<HolidayDto?> GetByIdAsync(int holidayId, CancellationToken cancellationToken = default);
    Task<HolidayDto> CreateAsync(CreateHolidayDto dto, CancellationToken cancellationToken = default);
    Task<HolidayDto?> UpdateAsync(int holidayId, UpdateHolidayDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int holidayId, CancellationToken cancellationToken = default);
    Task<bool> ExistsOnDateAsync(DateTime date, int? excludeHolidayId = null, CancellationToken cancellationToken = default);
}

public interface IShiftRepository
{
    Task<IReadOnlyList<ShiftDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ShiftDto?> GetByIdAsync(int shiftId, CancellationToken cancellationToken = default);
    Task<ShiftDto?> UpdateAsync(int shiftId, string name, TimeSpan startTime, TimeSpan endTime, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int excludeShiftId, CancellationToken cancellationToken = default);
    /// <summary>Makes <paramref name="employeeIds"/> the complete roster of the shift; returns null if the shift does not exist.</summary>
    Task<ShiftDto?> SetEmployeesAsync(int shiftId, IReadOnlyCollection<int> employeeIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftEmployeeDto>> GetAssignableEmployeesAsync(CancellationToken cancellationToken = default);
}

public interface ILeaveRepository
{
    Task<IReadOnlyList<LeaveDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<LeaveDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LeaveDto> CreateAsync(CreateLeaveDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken cancellationToken = default);
}

public interface IPayrollPeriodRepository
{
    Task<SavePayrollPeriodResultDto> SaveAsync(SavePayrollPeriodRequestDto request, CancellationToken cancellationToken = default);
    Task<PayrollByDateRangeDto?> GetSavedByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<PayrollByDateRangeDto?> GetSavedByIdAsync(int payrollPeriodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavePayrollPeriodResultDto>> GetAllSavedPeriodsAsync(CancellationToken cancellationToken = default);
    /// <summary>Marks the payroll record as having had its payslip emailed. Returns false if the record was not found.</summary>
    Task<bool> MarkPayslipEmailSentAsync(int employeeId, int payrollPeriodId, CancellationToken cancellationToken = default);

    /// <summary>Change requests (the audit trail) for a period, newest first.</summary>
    Task<IReadOnlyList<PayrollChangeRequestDto>> GetChangeRequestsAsync(int payrollPeriodId, bool pendingOnly = false, bool includeProposed = false, CancellationToken cancellationToken = default);
    /// <summary>Periods that currently have unreviewed edits, most recently edited first.</summary>
    Task<IReadOnlyList<PayrollApprovalSummaryDto>> GetPendingApprovalSummariesAsync(CancellationToken cancellationToken = default);
    /// <summary>Clears the "approvers already emailed" flag so the next pending change triggers a fresh email.</summary>
    Task ResetApprovalNotificationAsync(int payrollPeriodId, CancellationToken cancellationToken = default);
    /// <summary>Applies a pending change request to the live payroll record. Throws if it is not pending.</summary>
    Task<PayrollChangeRequestDto> ApproveChangeAsync(int changeRequestId, string reviewer, string? note, CancellationToken cancellationToken = default);
    /// <summary>Rejects a pending change request, leaving the live payroll record untouched.</summary>
    Task<PayrollChangeRequestDto> RejectChangeAsync(int changeRequestId, string reviewer, string? note, CancellationToken cancellationToken = default);
}



public class AuditLogDto

{

    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

}


