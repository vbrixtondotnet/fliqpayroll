using FliqPayroll.Core.DTOs;



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

public interface IPayrollPeriodRepository
{
    Task<SavePayrollPeriodResultDto> SaveAsync(SavePayrollPeriodRequestDto request, CancellationToken cancellationToken = default);
    Task<PayrollByDateRangeDto?> GetSavedByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<PayrollByDateRangeDto?> GetSavedByIdAsync(int payrollPeriodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavePayrollPeriodResultDto>> GetAllSavedPeriodsAsync(CancellationToken cancellationToken = default);
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


