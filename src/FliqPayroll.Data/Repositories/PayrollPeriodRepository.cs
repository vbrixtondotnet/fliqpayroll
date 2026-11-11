using System.Text.Json;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Enums;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FliqPayroll.Data.Repositories;

public class PayrollPeriodRepository : IPayrollPeriodRepository
{
    private readonly FliqPayrollDbContext _context;

    public PayrollPeriodRepository(FliqPayrollDbContext context)
    {
        _context = context;
    }

    public async Task<SavePayrollPeriodResultDto> SaveAsync(
        SavePayrollPeriodRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var startDate = PhilippineTime.ForDateStorage(request.FromDate);
        var endDate = PhilippineTime.ForDateStorage(request.ToDate);
        var requestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "Anonymous" : request.RequestedBy.Trim();

        var period = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(
                p => p.StartDate == startDate && p.EndDate == endDate,
                cancellationToken);

        var submitted = 0;

        if (period is null)
        {
            // First save = payroll generation. Nothing to approve yet.
            period = new PayrollPeriod
            {
                Name = request.PeriodName.Trim(),
                StartDate = startDate,
                EndDate = endDate,
                CutoffDay = endDate.Day,
                Status = PayrollPeriodStatus.Locked,
                IsLocked = true,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var record in request.Records)
            {
                var entity = MapToEntity(record, period.Id);
                entity.PayslipEmailSentAt = record.PayslipEmailSentAt;
                period.PayrollRecords.Add(entity);
            }

            _context.PayrollPeriods.Add(period);
        }
        else
        {
            // The payroll already exists, so manual edits must be approved before they take effect.
            // Live records stay untouched; each changed row becomes a pending change request.
            var liveByEmployee = period.PayrollRecords.ToDictionary(r => r.EmployeeId);
            var pendingByEmployee = (await _context.PayrollChangeRequests
                    .Where(c => c.PayrollPeriodId == period.Id && c.Status == PayrollChangeStatus.Pending)
                    .ToListAsync(cancellationToken))
                .GroupBy(c => c.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var now = DateTime.UtcNow;
            var addedRecords = false;

            foreach (var record in request.Records)
            {
                if (!liveByEmployee.TryGetValue(record.EmployeeId, out var live))
                {
                    // Employee was not part of the generated payroll; there is nothing to protect.
                    period.PayrollRecords.Add(MapToEntity(record, period.Id));
                    addedRecords = true;
                    continue;
                }

                var changes = PayrollChangeDetector.Compare(live, record);

                // Re-submitting replaces the previous unreviewed submission for the same employee.
                if (pendingByEmployee.TryGetValue(record.EmployeeId, out var existingPending))
                {
                    foreach (var old in existingPending)
                    {
                        old.Status = PayrollChangeStatus.Superseded;
                        old.ReviewedBy = requestedBy;
                        old.ReviewedAt = now;
                        old.ReviewNote = changes.Count == 0
                            ? "Edits were reverted to the approved values."
                            : "Replaced by a newer submission.";
                    }
                }

                if (changes.Count == 0)
                {
                    continue;
                }

                _context.PayrollChangeRequests.Add(new PayrollChangeRequest
                {
                    PayrollPeriod = period,
                    EmployeeId = live.EmployeeId,
                    EmployeeCode = live.EmployeeCode,
                    EmployeeName = live.EmployeeName,
                    Status = PayrollChangeStatus.Pending,
                    ProposedJson = JsonSerializer.Serialize(record),
                    ChangesJson = JsonSerializer.Serialize(changes),
                    RequestedBy = requestedBy,
                    RequestedAt = now
                });
                submitted++;
            }

            period.Name = request.PeriodName.Trim();
            if (addedRecords)
            {
                period.UpdatedAt = now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var pendingAfterSave = await CountPendingAsync(period.Id, cancellationToken);
        var notifyApprovers = false;

        if (pendingAfterSave == 0)
        {
            // Everything has been resolved (or edits were reverted): the next change should email again.
            await ResetApprovalNotificationAsync(period.Id, cancellationToken);
        }
        else if (submitted > 0)
        {
            // Atomic compare-and-set so concurrent saves can never both claim the notification.
            var now = DateTime.UtcNow;
            notifyApprovers = await _context.PayrollPeriods
                .Where(p => p.Id == period.Id && p.ApprovalNotifiedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ApprovalNotifiedAt, now), cancellationToken) == 1;
        }

        return new SavePayrollPeriodResultDto
        {
            NotifyApprovers = notifyApprovers,
            PayrollPeriodId = period.Id,
            PeriodName = period.Name,
            FromDate = PhilippineTime.ToPhilippineDate(period.StartDate),
            ToDate = PhilippineTime.ToPhilippineDate(period.EndDate),
            RecordCount = period.PayrollRecords.Count,
            SubmittedForApprovalCount = submitted,
            PendingApprovalCount = pendingAfterSave
        };
    }

    private async Task<int> CountPendingAsync(int payrollPeriodId, CancellationToken cancellationToken) =>
        (await _context.PayrollChangeRequests
            .Where(c => c.PayrollPeriodId == payrollPeriodId && c.Status == PayrollChangeStatus.Pending)
            .Select(c => c.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken)).Count;

    /// <summary>Flags rows that have an unreviewed edit so the UI can highlight them and block payslips.</summary>
    private async Task ApplyPendingFlagsAsync(PayrollByDateRangeDto dto, CancellationToken cancellationToken)
    {
        if (dto.PayrollPeriodId is not int periodId)
        {
            return;
        }

        var pending = await _context.PayrollChangeRequests
            .AsNoTracking()
            .Where(c => c.PayrollPeriodId == periodId && c.Status == PayrollChangeStatus.Pending)
            .Select(c => new { c.Id, c.EmployeeId })
            .ToListAsync(cancellationToken);

        var latestByEmployee = pending
            .GroupBy(c => c.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Max(c => c.Id));

        foreach (var record in dto.Records)
        {
            if (latestByEmployee.TryGetValue(record.EmployeeId, out var requestId))
            {
                record.HasPendingApproval = true;
                record.PendingChangeRequestId = requestId;
            }
        }

        dto.PendingApprovalCount = latestByEmployee.Count;
    }

    private async Task<PayrollByDateRangeDto> MapWithPendingAsync(PayrollPeriod period, CancellationToken cancellationToken)
    {
        var dto = MapPeriodToDto(period);
        await ApplyPendingFlagsAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyList<PayrollChangeRequestDto>> GetChangeRequestsAsync(
        int payrollPeriodId,
        bool pendingOnly = false,
        bool includeProposed = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollChangeRequests
            .AsNoTracking()
            .Include(c => c.PayrollPeriod)
            .Where(c => c.PayrollPeriodId == payrollPeriodId);

        if (pendingOnly)
        {
            query = query.Where(c => c.Status == PayrollChangeStatus.Pending);
        }

        var rows = await query
            .OrderByDescending(c => c.RequestedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(c => MapChangeRequest(c, includeProposed)).ToList();
    }

    public async Task<PayrollChangeRequestDto> ApproveChangeAsync(
        int changeRequestId,
        string reviewer,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadPendingRequestAsync(changeRequestId, cancellationToken);

        var live = await _context.PayrollRecords.FirstOrDefaultAsync(
            r => r.PayrollPeriodId == request.PayrollPeriodId && r.EmployeeId == request.EmployeeId,
            cancellationToken)
            ?? throw new InvalidOperationException("The payroll record for this change no longer exists.");

        var proposed = JsonSerializer.Deserialize<PayrollDto>(request.ProposedJson)
            ?? throw new InvalidOperationException("The proposed payroll data could not be read.");

        PayrollChangeDetector.Apply(live, proposed);
        Review(request, PayrollChangeStatus.Approved, reviewer, note);

        await _context.SaveChangesAsync(cancellationToken);
        await ResetNotificationIfResolvedAsync(request.PayrollPeriodId, cancellationToken);
        return MapChangeRequest(request, includeProposed: false);
    }

    public async Task<PayrollChangeRequestDto> RejectChangeAsync(
        int changeRequestId,
        string reviewer,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadPendingRequestAsync(changeRequestId, cancellationToken);
        Review(request, PayrollChangeStatus.Rejected, reviewer, note);

        await _context.SaveChangesAsync(cancellationToken);
        await ResetNotificationIfResolvedAsync(request.PayrollPeriodId, cancellationToken);
        return MapChangeRequest(request, includeProposed: false);
    }

    private async Task ResetNotificationIfResolvedAsync(int payrollPeriodId, CancellationToken cancellationToken)
    {
        if (await CountPendingAsync(payrollPeriodId, cancellationToken) == 0)
        {
            await ResetApprovalNotificationAsync(payrollPeriodId, cancellationToken);
        }
    }

    public Task ResetApprovalNotificationAsync(int payrollPeriodId, CancellationToken cancellationToken = default) =>
        _context.PayrollPeriods
            .Where(p => p.Id == payrollPeriodId && p.ApprovalNotifiedAt != null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ApprovalNotifiedAt, (DateTime?)null), cancellationToken);

    public async Task<IReadOnlyList<PayrollApprovalSummaryDto>> GetPendingApprovalSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        var pending = await _context.PayrollChangeRequests
            .AsNoTracking()
            .Include(c => c.PayrollPeriod)
            .Where(c => c.Status == PayrollChangeStatus.Pending)
            .ToListAsync(cancellationToken);

        return pending
            .GroupBy(c => c.PayrollPeriodId)
            .Select(g =>
            {
                var period = g.First().PayrollPeriod;
                return new PayrollApprovalSummaryDto
                {
                    PayrollPeriodId = g.Key,
                    PeriodName = period.Name,
                    FromDate = PhilippineTime.ToPhilippineDate(period.StartDate),
                    ToDate = PhilippineTime.ToPhilippineDate(period.EndDate),
                    PendingEmployeeCount = g.Select(c => c.EmployeeId).Distinct().Count(),
                    PendingChangeCount = g.Sum(c =>
                        JsonSerializer.Deserialize<List<PayrollFieldChangeDto>>(c.ChangesJson)?.Count ?? 0),
                    LatestRequestedAt = DateTime.SpecifyKind(g.Max(c => c.RequestedAt), DateTimeKind.Utc),
                    RequestedBy = g.Select(c => c.RequestedBy).Distinct().OrderBy(n => n).ToList()
                };
            })
            .OrderByDescending(s => s.LatestRequestedAt)
            .ToList();
    }

    private async Task<PayrollChangeRequest> LoadPendingRequestAsync(int id, CancellationToken cancellationToken)
    {
        var request = await _context.PayrollChangeRequests
            .Include(c => c.PayrollPeriod)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ArgumentException("Change request not found.");

        if (request.Status != PayrollChangeStatus.Pending)
        {
            throw new InvalidOperationException(
                $"This change request has already been {request.Status.ToString().ToLowerInvariant()}.");
        }

        return request;
    }

    private static void Review(PayrollChangeRequest request, PayrollChangeStatus status, string reviewer, string? note)
    {
        request.Status = status;
        request.ReviewedBy = string.IsNullOrWhiteSpace(reviewer) ? "Unknown" : reviewer.Trim();
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private static PayrollChangeRequestDto MapChangeRequest(PayrollChangeRequest c, bool includeProposed) => new()
    {
        Id = c.Id,
        PayrollPeriodId = c.PayrollPeriodId,
        PeriodName = c.PayrollPeriod?.Name ?? string.Empty,
        EmployeeId = c.EmployeeId,
        EmployeeCode = c.EmployeeCode,
        EmployeeName = c.EmployeeName,
        Status = c.Status,
        RequestedBy = c.RequestedBy,
        RequestedAt = DateTime.SpecifyKind(c.RequestedAt, DateTimeKind.Utc),
        ReviewedBy = c.ReviewedBy,
        ReviewedAt = c.ReviewedAt.HasValue ? DateTime.SpecifyKind(c.ReviewedAt.Value, DateTimeKind.Utc) : null,
        ReviewNote = c.ReviewNote,
        Changes = JsonSerializer.Deserialize<List<PayrollFieldChangeDto>>(c.ChangesJson) ?? [],
        ProposedRecord = includeProposed ? JsonSerializer.Deserialize<PayrollDto>(c.ProposedJson) : null
    };

    public async Task<PayrollByDateRangeDto?> GetSavedByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var startDate = PhilippineTime.ForDateStorage(fromDate);
        var endDate = PhilippineTime.ForDateStorage(toDate);

        var period = await _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(
                p => p.StartDate == startDate && p.EndDate == endDate,
                cancellationToken);

        return period is null ? null : await MapWithPendingAsync(period, cancellationToken);
    }

    public async Task<PayrollByDateRangeDto?> GetSavedByIdAsync(
        int payrollPeriodId,
        CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.Id == payrollPeriodId, cancellationToken);

        return period is null ? null : await MapWithPendingAsync(period, cancellationToken);
    }

    public async Task<IReadOnlyList<SavePayrollPeriodResultDto>> GetAllSavedPeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        var periods = await _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.PayrollRecords)
            .OrderByDescending(p => p.StartDate)
            .ThenByDescending(p => p.EndDate)
            .ToListAsync(cancellationToken);

        var pendingCounts = (await _context.PayrollChangeRequests
                .AsNoTracking()
                .Where(c => c.Status == PayrollChangeStatus.Pending)
                .Select(c => new { c.PayrollPeriodId, c.EmployeeId })
                .Distinct()
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.PayrollPeriodId)
            .ToDictionary(g => g.Key, g => g.Count());

        return periods
            .Select(p => new SavePayrollPeriodResultDto
            {
                PendingApprovalCount = pendingCounts.GetValueOrDefault(p.Id),
                PayrollPeriodId = p.Id,
                PeriodName = p.Name,
                FromDate = PhilippineTime.ToPhilippineDate(p.StartDate),
                ToDate = PhilippineTime.ToPhilippineDate(p.EndDate),
                RecordCount = p.PayrollRecords.Count
            })
            .ToList();
    }

    public async Task<bool> MarkPayslipEmailSentAsync(
        int employeeId,
        int payrollPeriodId,
        CancellationToken cancellationToken = default)
    {
        var record = await _context.PayrollRecords
            .FirstOrDefaultAsync(
                r => r.EmployeeId == employeeId && r.PayrollPeriodId == payrollPeriodId,
                cancellationToken);

        if (record is null)
        {
            return false;
        }

        record.PayslipEmailSentAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PayrollByDateRangeDto MapPeriodToDto(PayrollPeriod period) => new()
    {
        PayrollPeriodId = period.Id,
        FromDate = PhilippineTime.ToPhilippineDate(period.StartDate),
        ToDate = PhilippineTime.ToPhilippineDate(period.EndDate),
        PeriodName = period.Name,
        Records = period.PayrollRecords
            .OrderBy(r => r.EmployeeName)
            .Select(MapToDto)
            .ToList()
    };

    private static PayrollRecord MapToEntity(PayrollDto dto, int payrollPeriodId) => new()
    {
        PayrollPeriodId = payrollPeriodId,
        EmployeeId = dto.EmployeeId,
        EmployeeName = dto.EmployeeName,
        EmployeeCode = dto.EmployeeCode,
        Position = dto.Position,
        SalaryType = dto.SalaryType,
        BasicSalary = dto.BasicSalary,
        MonthlySalary = dto.MonthlySalary,
        BiMonthlySalary = dto.BiMonthlySalary,
        DailyRate = dto.DailyRate,
        HourlyRate = dto.HourlyRate,
        WorkingDays = dto.WorkingDays,
        AbsentDays = dto.AbsentDays,
        AbsentAmount = dto.AbsentAmount,
        BasicPayAmount = dto.BasicPayAmount,
        GrossSalary = dto.GrossSalary,
        RegularOtRate = dto.RegularOtRate,
        RegularOtHours = dto.RegularOtHours,
        SpecialOtRate = dto.SpecialOtRate,
        SpecialOtHours = dto.SpecialOtHours,
        SpecialOtPay = dto.SpecialOtPay,
        HolidayOtRate = dto.HolidayOtRate,
        HolidayDays = dto.HolidayDays,
        HolidayOtPay = dto.HolidayOtPay,
        NightDiffOtRate = dto.NightDiffOtRate,
        NightDiffHours = dto.NightDiffHours,
        NightDiffOtPay = dto.NightDiffOtPay,
        LeaveDays = dto.LeaveDays,
        OvertimePay = dto.OvertimePay,
        HolidayPay = dto.HolidayPay,
        RegularHolidayPay = dto.RegularHolidayPay,
        SpecialNonWorkingPay = dto.SpecialNonWorkingPay,
        LeaveWithPay = dto.LeaveWithPay,
        Incentives = dto.Incentives,
        Allowances = dto.Allowances,
        Bonuses = dto.Bonuses,
        AdjustmentsEarnings = dto.AdjustmentsEarnings,
        GrossPay = dto.GrossPay,
        AbsenceDeduction = dto.AbsenceDeduction,
        LateDeduction = dto.LateDeduction,
        LateUndertimeHours = dto.LateUndertimeHours,
        LateUndertimeAmount = dto.LateUndertimeAmount,
        UndertimeDeduction = dto.UndertimeDeduction,
        CashAdvance = dto.CashAdvance,
        SssDeduction = dto.SssDeduction,
        PhilHealthDeduction = dto.PhilHealthDeduction,
        PagIbigDeduction = dto.PagIbigDeduction,
        SssLoanDeduction = dto.SssLoanDeduction,
        SssCalamityDeduction = dto.SssCalamityDeduction,
        PagIbigLoanDeduction = dto.PagIbigLoanDeduction,
        WithholdingTax = dto.WithholdingTax,
        OtherDeductions = dto.OtherDeductions,
        ToAdd = dto.ToAdd,
        ToDeduct = dto.ToDeduct,
        TotalDeductions = dto.TotalDeductions,
        NetPay = dto.NetPay,
        PaymentMethod = dto.PaymentMethod,
        Status = dto.Status,
        PayslipEmailSentAt = dto.PayslipEmailSentAt,
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
    };

    private static PayrollDto MapToDto(PayrollRecord entity) => new()
    {
        EmployeeId = entity.EmployeeId,
        EmployeeName = entity.EmployeeName,
        EmployeeCode = entity.EmployeeCode,
        Position = entity.Position,
        SalaryType = entity.SalaryType,
        BasicSalary = entity.BasicSalary,
        MonthlySalary = entity.MonthlySalary,
        BiMonthlySalary = entity.BiMonthlySalary,
        DailyRate = entity.DailyRate,
        HourlyRate = entity.HourlyRate,
        WorkingDays = entity.WorkingDays,
        AbsentDays = entity.AbsentDays,
        AbsentAmount = entity.AbsentAmount,
        BasicPayAmount = entity.BasicPayAmount,
        GrossSalary = entity.GrossSalary,
        RegularOtRate = entity.RegularOtRate,
        RegularOtHours = entity.RegularOtHours,
        SpecialOtRate = entity.SpecialOtRate,
        SpecialOtHours = entity.SpecialOtHours,
        SpecialOtPay = entity.SpecialOtPay,
        HolidayOtRate = entity.HolidayOtRate,
        HolidayDays = entity.HolidayDays,
        HolidayOtPay = entity.HolidayOtPay,
        NightDiffOtRate = entity.NightDiffOtRate,
        NightDiffHours = entity.NightDiffHours,
        NightDiffOtPay = entity.NightDiffOtPay,
        LeaveDays = entity.LeaveDays,
        OvertimePay = entity.OvertimePay,
        HolidayPay = entity.HolidayPay,
        RegularHolidayPay = entity.RegularHolidayPay,
        SpecialNonWorkingPay = entity.SpecialNonWorkingPay,
        LeaveWithPay = entity.LeaveWithPay,
        Incentives = entity.Incentives,
        Allowances = entity.Allowances,
        Bonuses = entity.Bonuses,
        AdjustmentsEarnings = entity.AdjustmentsEarnings,
        GrossPay = entity.GrossPay,
        AbsenceDeduction = entity.AbsenceDeduction,
        LateDeduction = entity.LateDeduction,
        LateUndertimeHours = entity.LateUndertimeHours,
        LateUndertimeAmount = entity.LateUndertimeAmount,
        UndertimeDeduction = entity.UndertimeDeduction,
        CashAdvance = entity.CashAdvance,
        SssDeduction = entity.SssDeduction,
        PhilHealthDeduction = entity.PhilHealthDeduction,
        PagIbigDeduction = entity.PagIbigDeduction,
        SssLoanDeduction = entity.SssLoanDeduction,
        SssCalamityDeduction = entity.SssCalamityDeduction,
        PagIbigLoanDeduction = entity.PagIbigLoanDeduction,
        WithholdingTax = entity.WithholdingTax,
        OtherDeductions = entity.OtherDeductions,
        ToAdd = entity.ToAdd,
        ToDeduct = entity.ToDeduct,
        TotalDeductions = entity.TotalDeductions,
        NetPay = entity.NetPay,
        PaymentMethod = entity.PaymentMethod,
        Status = entity.Status,
        PayslipEmailSentAt = entity.PayslipEmailSentAt,
        PayslipEmailSent = entity.PayslipEmailSentAt.HasValue
    };
}
