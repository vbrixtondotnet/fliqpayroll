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

        var period = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(
                p => p.StartDate == startDate && p.EndDate == endDate,
                cancellationToken);

        // Preserve email-sent timestamps across re-save (records are wiped and re-inserted).
        var preservedEmailSentAt = period?.PayrollRecords
            .Where(r => r.PayslipEmailSentAt.HasValue)
            .ToDictionary(r => r.EmployeeId, r => r.PayslipEmailSentAt)
            ?? new Dictionary<int, DateTime?>();

        if (period is null)
        {
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

            _context.PayrollPeriods.Add(period);
        }
        else
        {
            period.Name = request.PeriodName.Trim();
            period.CutoffDay = endDate.Day;
            period.Status = PayrollPeriodStatus.Locked;
            period.IsLocked = true;
            period.UpdatedAt = DateTime.UtcNow;
            _context.PayrollRecords.RemoveRange(period.PayrollRecords);
        }

        foreach (var record in request.Records)
        {
            var entity = MapToEntity(record, period.Id);

            if (preservedEmailSentAt.TryGetValue(record.EmployeeId, out var preservedSentAt)
                && preservedSentAt.HasValue)
            {
                entity.PayslipEmailSentAt = preservedSentAt;
            }
            else if (record.PayslipEmailSentAt.HasValue)
            {
                entity.PayslipEmailSentAt = record.PayslipEmailSentAt;
            }

            period.PayrollRecords.Add(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SavePayrollPeriodResultDto
        {
            PayrollPeriodId = period.Id,
            PeriodName = period.Name,
            FromDate = PhilippineTime.ToPhilippineDate(period.StartDate),
            ToDate = PhilippineTime.ToPhilippineDate(period.EndDate),
            RecordCount = period.PayrollRecords.Count
        };
    }

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

        return period is null ? null : MapPeriodToDto(period);
    }

    public async Task<PayrollByDateRangeDto?> GetSavedByIdAsync(
        int payrollPeriodId,
        CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.Id == payrollPeriodId, cancellationToken);

        return period is null ? null : MapPeriodToDto(period);
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

        return periods
            .Select(p => new SavePayrollPeriodResultDto
            {
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
