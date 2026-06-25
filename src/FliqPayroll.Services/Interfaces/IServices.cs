using FliqPayroll.Core.DTOs;



namespace FliqPayroll.Services.Interfaces;



public interface IDashboardService

{

    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

}



public interface IEmployeeService

{

    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(EmployeeFilterDto? filter = null, CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default);

    Task<EmployeeDto?> UpdateAsync(UpdateEmployeeDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

}



public interface IAttendanceService

{

    Task<IReadOnlyList<AttendanceDto>> GetSheetAsync(DateTime date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<AttendanceDto?> UpdateAsync(UpdateAttendanceDto dto, CancellationToken cancellationToken = default);

    Task<AttendanceUploadResultDto> UploadCsvAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    Task<AttendanceSummaryDto> GetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

}



public interface IPayrollService

{

    Task<PayrollPeriodDto> GetDefaultPeriodAsync(DateTime? referenceDate = null, CancellationToken cancellationToken = default);

    Task<PayrollByDateRangeDto> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<SavePayrollPeriodResultDto> SavePeriodAsync(SavePayrollPeriodRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavePayrollPeriodResultDto>> GetSavedPeriodsAsync(CancellationToken cancellationToken = default);

    Task<PayrollByDateRangeDto?> GetSavedPeriodByIdAsync(int payrollPeriodId, CancellationToken cancellationToken = default);

}



public interface IBiometricService

{

    Task<AttendanceUploadResultDto> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

}



public interface IReportService

{

    Task<PayrollSummaryReportDto> GetPayrollSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<PayrollSummaryReportDto> GetPayrollSummaryByPeriodIdAsync(int payrollPeriodId, CancellationToken cancellationToken = default);

    Task<PayslipDto?> GetPayslipAsync(int employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<PayslipDto?> GetPayslipByPeriodIdAsync(int employeeId, int payrollPeriodId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayslipDto>> GetAllPayslipsByPeriodIdAsync(int payrollPeriodId, CancellationToken cancellationToken = default);

    Task<EmployeePayrollHistoryDto?> GetEmployeeHistoryAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportPayrollSummaryCsvAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<byte[]> ExportEmployeesCsvAsync(EmployeeFilterDto? filter = null, CancellationToken cancellationToken = default);

}



public interface IHolidayService

{

    Task<IReadOnlyList<HolidayDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<HolidayDto?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);

    Task<HolidayDto> AddAsync(CreateHolidayDto dto, CancellationToken cancellationToken = default);

    Task<HolidayDto?> UpdateAsync(int holidayId, UpdateHolidayDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int holidayId, CancellationToken cancellationToken = default);

}

public interface ILeaveService
{
    Task<IReadOnlyList<LeaveDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<LeaveDto> CreateAsync(CreateLeaveDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}


