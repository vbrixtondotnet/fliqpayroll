using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Enums;
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
    private readonly IShiftRepository _shiftRepository;

    public PayrollService(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        IHolidayRepository holidayRepository,
        ILeaveRepository leaveRepository,
        IPayrollPeriodRepository payrollPeriodRepository,
        IShiftRepository shiftRepository)
    {
        _shiftRepository = Guard.AgainstNull(shiftRepository, nameof(shiftRepository));
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
        var computed = await ComputePayrollForPeriodAsync(period, employeeId: null, cancellationToken);

        var saved = await _payrollPeriodRepository.GetSavedByDateRangeAsync(fromDate, toDate, cancellationToken);
        if (saved?.PayrollPeriodId is not int savedPeriodId)
        {
            return new PayrollByDateRangeDto
            {
                FromDate = period.StartDate,
                ToDate = period.EndDate,
                PeriodName = period.Name,
                Records = computed
            };
        }

        // The period was already generated and saved: show the approved (live) values, with any
        // unreviewed edit overlaid and flagged, instead of silently recalculating over them.
        var pendingByEmployee = (await _payrollPeriodRepository.GetChangeRequestsAsync(
                savedPeriodId, pendingOnly: true, includeProposed: true, cancellationToken))
            .Where(c => c.ProposedRecord is not null)
            .GroupBy(c => c.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Id).First());
        var savedByEmployee = saved.Records.ToDictionary(r => r.EmployeeId);

        var merged = new List<PayrollDto>(computed.Count);
        foreach (var fresh in computed)
        {
            if (!savedByEmployee.TryGetValue(fresh.EmployeeId, out var liveRecord))
            {
                merged.Add(fresh);
                continue;
            }

            var row = liveRecord;
            if (pendingByEmployee.TryGetValue(fresh.EmployeeId, out var pending))
            {
                row = pending.ProposedRecord!;
                row.HasPendingApproval = true;
                row.PendingChangeRequestId = pending.Id;
            }

            // Display-only data that is not persisted with the saved record.
            row.ShiftName = fresh.ShiftName;
            row.AbsentDates = fresh.AbsentDates;
            row.RegularHolidayAbsentCount = fresh.RegularHolidayAbsentCount;
            row.Email = fresh.Email;
            merged.Add(row);
        }

        return new PayrollByDateRangeDto
        {
            PayrollPeriodId = savedPeriodId,
            FromDate = period.StartDate,
            ToDate = period.EndDate,
            PeriodName = saved.PeriodName,
            PendingApprovalCount = merged.Count(r => r.HasPendingApproval),
            Records = merged
        };
    }

    public Task<IReadOnlyList<PayrollChangeRequestDto>> GetChangeRequestsAsync(
        int payrollPeriodId,
        bool pendingOnly = false,
        CancellationToken cancellationToken = default) =>
        _payrollPeriodRepository.GetChangeRequestsAsync(payrollPeriodId, pendingOnly, false, cancellationToken);

    public Task<PayrollChangeRequestDto> ApproveChangeAsync(
        int changeRequestId,
        string reviewer,
        string? note,
        CancellationToken cancellationToken = default) =>
        _payrollPeriodRepository.ApproveChangeAsync(changeRequestId, reviewer, note, cancellationToken);

    public Task<IReadOnlyList<PayrollApprovalSummaryDto>> GetPendingApprovalSummariesAsync(
        CancellationToken cancellationToken = default) =>
        _payrollPeriodRepository.GetPendingApprovalSummariesAsync(cancellationToken);

    public async Task<int> ReviewAllPendingAsync(
        int payrollPeriodId,
        bool approve,
        string reviewer,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var pending = await _payrollPeriodRepository.GetChangeRequestsAsync(
            payrollPeriodId, pendingOnly: true, includeProposed: false, cancellationToken);

        var reviewed = 0;
        foreach (var request in pending)
        {
            try
            {
                if (approve)
                {
                    await _payrollPeriodRepository.ApproveChangeAsync(request.Id, reviewer, note, cancellationToken);
                }
                else
                {
                    await _payrollPeriodRepository.RejectChangeAsync(request.Id, reviewer, note, cancellationToken);
                }

                reviewed++;
            }
            catch (InvalidOperationException)
            {
                // Already reviewed by someone else in the meantime; skip it.
            }
        }

        return reviewed;
    }

    public Task<PayrollChangeRequestDto> RejectChangeAsync(
        int changeRequestId,
        string reviewer,
        string? note,
        CancellationToken cancellationToken = default) =>
        _payrollPeriodRepository.RejectChangeAsync(changeRequestId, reviewer, note, cancellationToken);

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

            var payroll = PayrollCalculator.Compute(employee, attendance, period, holidays, employeeLeaves);

            // Fixed salary: never include holiday pay; cutoff pay stays BasicSalary / 2.
            if (employee.SalaryType == SalaryType.Fixed)
            {
                payroll.HolidayPay = 0m;
                payroll.RegularHolidayPay = 0m;
                payroll.SpecialNonWorkingPay = 0m;
                payroll.HolidayOtPay = 0m;
                payroll.SpecialOtPay = 0m;
                payroll.SpecialOtHours = 0m;
                payroll.HolidayDays = 0m;
                payroll.RegularHolidayAbsentCount = 0;
                payroll.GrossPay = payroll.BasicPayAmount + payroll.NightDiffOtPay;
            }

            results.Add(payroll);
        }

        await ApplyShiftNamesAsync(results, cancellationToken);
        return results;
    }

    /// <summary>Labels each row with the employee's current shift; unassigned employees show the default Day Shift.</summary>
    private async Task ApplyShiftNamesAsync(IEnumerable<PayrollDto> records, CancellationToken cancellationToken)
    {
        var shifts = await _shiftRepository.GetAllAsync(cancellationToken);
        var defaultName = shifts.FirstOrDefault(s => s.Id == ShiftConstants.DefaultShiftId)?.Name
            ?? ShiftConstants.DefaultShiftName;
        var shiftByEmployee = shifts
            .SelectMany(s => s.Employees.Select(e => (e.Id, s.Name)))
            .ToDictionary(x => x.Id, x => x.Name);

        foreach (var record in records)
        {
            record.ShiftName = shiftByEmployee.GetValueOrDefault(record.EmployeeId, defaultName);
        }
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
                Records = request.Records,
                RequestedBy = request.RequestedBy
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<SavePayrollPeriodResultDto>> GetSavedPeriodsAsync(
        CancellationToken cancellationToken = default) =>
        _payrollPeriodRepository.GetAllSavedPeriodsAsync(cancellationToken);

    public async Task<PayrollByDateRangeDto?> GetSavedPeriodByIdAsync(
        int payrollPeriodId,
        CancellationToken cancellationToken = default)
    {
        var saved = await _payrollPeriodRepository.GetSavedByIdAsync(payrollPeriodId, cancellationToken);
        if (saved is not null)
        {
            await ApplyShiftNamesAsync(saved.Records, cancellationToken);
        }

        return saved;
    }

    public async Task<byte[]> ExportExcelAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var data = await GetExportDataAsync(fromDate, toDate, cancellationToken);
        return PayrollExportBuilder.BuildExcel(data);
    }

    public async Task<byte[]> ExportCsvAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var data = await GetExportDataAsync(fromDate, toDate, cancellationToken);
        return PayrollExportBuilder.BuildCsv(data);
    }

    public async Task<byte[]> ExportPdfAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var data = await GetExportDataAsync(fromDate, toDate, cancellationToken);
        return PayrollExportBuilder.BuildPdf(data);
    }

    private async Task<PayrollByDateRangeDto> GetExportDataAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        var saved = await _payrollPeriodRepository.GetSavedByDateRangeAsync(
            fromDate,
            toDate,
            cancellationToken);

        return saved ?? await GetByDateRangeAsync(fromDate, toDate, cancellationToken);
    }
}
