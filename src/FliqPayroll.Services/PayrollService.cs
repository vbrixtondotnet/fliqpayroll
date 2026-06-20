using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;

namespace FliqPayroll.Services;

public class PayrollService : IPayrollService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IHolidayRepository _holidayRepository;

    public PayrollService(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        IHolidayRepository holidayRepository)
    {
        _employeeRepository = Guard.AgainstNull(employeeRepository, nameof(employeeRepository));
        _attendanceRepository = Guard.AgainstNull(attendanceRepository, nameof(attendanceRepository));
        _holidayRepository = Guard.AgainstNull(holidayRepository, nameof(holidayRepository));
    }

    public Task<PayrollPeriodDto> GetDefaultPeriodAsync(DateTime? referenceDate = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(PayrollPeriodHelper.ResolveDefaultPeriod(referenceDate ?? PhilippineTime.Today));

    public async Task<PayrollByDateRangeDto> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var period = PayrollPeriodHelper.CreateRange(fromDate, toDate);
        var records = await ComputePayrollForPeriodAsync(period, employeeId: null, cancellationToken);

        return new PayrollByDateRangeDto
        {
            FromDate = period.StartDate,
            ToDate = period.EndDate,
            PeriodName = period.Name,
            Records = records
        };
    }

    private async Task<IReadOnlyList<PayrollDto>> ComputePayrollForPeriodAsync(
        PayrollPeriodDto period,
        int? employeeId,
        CancellationToken cancellationToken)
    {
        var employees = await _employeeRepository.GetAllAsync(
            new EmployeeFilterDto { IsActive = true },
            cancellationToken);

        var targetEmployees = employeeId.HasValue
            ? employees.Where(e => e.Id == employeeId.Value).ToList()
            : employees.ToList();

        if (employeeId.HasValue && targetEmployees.Count == 0)
        {
            throw new InvalidOperationException($"Active employee {employeeId.Value} was not found.");
        }

        var holidays = await _holidayRepository.GetByDateRangeAsync(
            period.StartDate,
            period.EndDate,
            cancellationToken);

        var results = new List<PayrollDto>();

        foreach (var employee in targetEmployees)
        {
            var attendance = await _attendanceRepository.GetByEmployeeAndPeriodAsync(
                employee.Id,
                period.StartDate,
                period.EndDate,
                cancellationToken);

            results.Add(PayrollCalculator.Compute(employee, attendance, period, holidays));
        }

        return results;
    }
}
