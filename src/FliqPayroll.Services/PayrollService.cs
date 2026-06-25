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
    private readonly ILeaveRepository _leaveRepository;
    private readonly IPayrollPeriodRepository _payrollPeriodRepository;

    public PayrollService(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        IHolidayRepository holidayRepository,
        ILeaveRepository leaveRepository,
        IPayrollPeriodRepository payrollPeriodRepository)
    {
        _employeeRepository = Guard.AgainstNull(employeeRepository, nameof(employeeRepository));
        _attendanceRepository = Guard.AgainstNull(attendanceRepository, nameof(attendanceRepository));
        _holidayRepository = Guard.AgainstNull(holidayRepository, nameof(holidayRepository));
        _leaveRepository = Guard.AgainstNull(leaveRepository, nameof(leaveRepository));
        _payrollPeriodRepository = Guard.AgainstNull(payrollPeriodRepository, nameof(payrollPeriodRepository));
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

        var leaves = await _leaveRepository.GetByDateRangeAsync(
            period.StartDate,
            period.EndDate,
            cancellationToken);

        var leavesByEmployee = leaves
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<LeaveDto>)g.ToList());

        var results = new List<PayrollDto>();

        foreach (var employee in targetEmployees)
        {
            var attendance = await _attendanceRepository.GetByEmployeeAndPeriodAsync(
                employee.Id,
                period.StartDate,
                period.EndDate,
                cancellationToken);

            leavesByEmployee.TryGetValue(employee.Id, out var employeeLeaves);
            employeeLeaves ??= Array.Empty<LeaveDto>();

            results.Add(PayrollCalculator.Compute(employee, attendance, period, holidays, employeeLeaves));
        }

        return results;
    }

    public async Task<SavePayrollPeriodResultDto> SavePeriodAsync(
        SavePayrollPeriodRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(request, nameof(request));

        if (request.Records is null || request.Records.Count == 0)
        {
            throw new ArgumentException("At least one payroll row is required.");
        }

        var fromDate = PhilippineTime.ToPhilippineDate(request.FromDate);
        var toDate = PhilippineTime.ToPhilippineDate(request.ToDate);

        if (toDate < fromDate)
        {
            throw new ArgumentException("To date must be on or after From date.");
        }

        if (string.IsNullOrWhiteSpace(request.PeriodName))
        {
            throw new ArgumentException("Period name is required.");
        }

        if (request.Records.Any(r => r.EmployeeId <= 0))
        {
            throw new ArgumentException("Each payroll row must include a valid employee.");
        }

        var employeeIds = request.Records.Select(r => r.EmployeeId).Distinct().ToList();
        var employees = await _employeeRepository.GetAllAsync(
            new EmployeeFilterDto { IsActive = true },
            cancellationToken);
        var validEmployeeIds = employees.Select(e => e.Id).ToHashSet();

        if (employeeIds.Any(id => !validEmployeeIds.Contains(id)))
        {
            throw new ArgumentException("One or more payroll rows reference an invalid employee.");
        }

        return await _payrollPeriodRepository.SaveAsync(
            new SavePayrollPeriodRequestDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                PeriodName = request.PeriodName.Trim(),
                Records = request.Records
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<SavePayrollPeriodResultDto>> GetSavedPeriodsAsync(
        CancellationToken cancellationToken = default) =>
        _payrollPeriodRepository.GetAllSavedPeriodsAsync(cancellationToken);

    public Task<PayrollByDateRangeDto?> GetSavedPeriodByIdAsync(
        int payrollPeriodId,
        CancellationToken cancellationToken = default) =>
        _payrollPeriodRepository.GetSavedByIdAsync(payrollPeriodId, cancellationToken);
}
