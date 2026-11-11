using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Data.Entities;
using Microsoft.EntityFrameworkCore;



namespace FliqPayroll.Data.Repositories;



public class AttendanceRepository : IAttendanceRepository

{

    private readonly FliqPayrollDbContext _context;



    public AttendanceRepository(FliqPayrollDbContext context)

    {

        _context = Guard.AgainstNull(context, nameof(context));

    }



    public async Task<IReadOnlyList<AttendanceDto>> GetByDateRangeAsync(

        DateTime startDate,

        DateTime endDate,

        CancellationToken cancellationToken = default)

    {

        var phStart = AttendanceDateHelper.ToCalendarDate(startDate);

        var phEnd = AttendanceDateHelper.ToCalendarDate(endDate);

        var endExclusive = phEnd.AddDays(1);



        var entities = await _context.AttendanceRecords

            .AsNoTracking()

            .Include(a => a.Employee)
            .ThenInclude(e => e.Shift)

            .Where(a => a.Date >= phStart && a.Date < endExclusive)

            .OrderBy(a => a.Employee.LastName)

            .ThenBy(a => a.Employee.FirstName)

            .ThenBy(a => a.Date)

            .ToListAsync(cancellationToken);



        var defaultSchedule = await LoadDefaultScheduleAsync(cancellationToken);
        return entities.Select(e => MapToDto(e, defaultSchedule)).ToList();

    }



    public async Task<IReadOnlyList<AttendanceDto>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)

    {

        var calendarDate = AttendanceDateHelper.ToCalendarDate(date);



        var entities = await _context.AttendanceRecords

            .AsNoTracking()

            .Include(a => a.Employee)
            .ThenInclude(e => e.Shift)

            .Where(a =>

                a.Date.Year == calendarDate.Year &&

                a.Date.Month == calendarDate.Month &&

                a.Date.Day == calendarDate.Day)

            .OrderBy(a => a.Employee.LastName)

            .ThenBy(a => a.Employee.FirstName)

            .ToListAsync(cancellationToken);



        var defaultSchedule = await LoadDefaultScheduleAsync(cancellationToken);
        return entities.Select(e => MapToDto(e, defaultSchedule)).ToList();

    }



    public async Task<AttendanceDto?> UpdateAsync(UpdateAttendanceDto dto, CancellationToken cancellationToken = default)

    {

        Guard.AgainstNull(dto, nameof(dto));



        var entity = await _context.AttendanceRecords

            .Include(a => a.Employee)
            .ThenInclude(e => e.Shift)

            .FirstOrDefaultAsync(a => a.Id == dto.Id, cancellationToken);



        if (entity is null)

        {

            return null;

        }



        entity.TimeIn = dto.TimeIn;

        entity.TimeOut = dto.TimeOut;

        entity.IsLate = dto.IsLate;

        entity.OvertimeIn = dto.OvertimeIn;

        entity.OvertimeOut = dto.OvertimeOut;

        entity.IsOvertimeValid = dto.IsOvertimeValid;

        entity.Notes = dto.Notes?.Trim();

        entity.UpdatedAt = DateTime.UtcNow;



        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity, await LoadDefaultScheduleAsync(cancellationToken));

    }



    public async Task<int> CountValidByDateAsync(DateTime date, CancellationToken cancellationToken = default)

    {

        var records = await GetByDateAsync(date, cancellationToken);

        return records.Count(r => r.IsAttendanceValid);

    }



    public async Task<int> CountLateByDateAsync(DateTime date, CancellationToken cancellationToken = default)

    {

        var records = await GetByDateAsync(date, cancellationToken);

        return records.Count(r => r.IsLate);

    }



    public async Task<IReadOnlyList<AttendanceDto>> GetByEmployeeAndPeriodAsync(

        int employeeId,

        DateTime startDate,

        DateTime endDate,

        CancellationToken cancellationToken = default)

    {

        var phStart = AttendanceDateHelper.ToCalendarDate(startDate);

        var phEnd = AttendanceDateHelper.ToCalendarDate(endDate);

        var endExclusive = phEnd.AddDays(1);



        var entities = await _context.AttendanceRecords

            .AsNoTracking()

            .Include(a => a.Employee)
            .ThenInclude(e => e.Shift)

            .Where(a =>

                a.EmployeeId == employeeId &&

                a.Date >= phStart &&

                a.Date < endExclusive)

            .OrderBy(a => a.Date)

            .ToListAsync(cancellationToken);



        var defaultSchedule = await LoadDefaultScheduleAsync(cancellationToken);
        return entities.Select(e => MapToDto(e, defaultSchedule)).ToList();

    }



    public async Task UpsertBiometricAsync(ProcessedAttendanceDayDto record, CancellationToken cancellationToken = default)

    {

        Guard.AgainstNull(record, nameof(record));



        var calendarDate = AttendanceDateHelper.ToCalendarDate(record.Date);



        var entity = await _context.AttendanceRecords

            .FirstOrDefaultAsync(

                a => a.EmployeeId == record.EmployeeId &&

                     a.Date.Year == calendarDate.Year &&

                     a.Date.Month == calendarDate.Month &&

                     a.Date.Day == calendarDate.Day,

                cancellationToken);



        if (entity is null)

        {

            entity = new AttendanceRecord

            {

                EmployeeId = record.EmployeeId,

                Date = calendarDate

            };

            _context.AttendanceRecords.Add(entity);

        }

        else if (entity.Date != calendarDate)

        {

            entity.Date = calendarDate;

        }



        entity.TimeIn = record.TimeIn;

        entity.TimeOut = record.TimeOut;

        entity.IsLate = record.IsLate;

        entity.OvertimeIn = record.OvertimeIn;

        entity.OvertimeOut = record.OvertimeOut;

        entity.IsOvertimeValid = record.IsOvertimeValid;

        entity.IsFromBiometric = true;

        entity.UpdatedAt = DateTime.UtcNow;



        await _context.SaveChangesAsync(cancellationToken);

    }



    public async Task<int> SaveUploadLogAsync(

        string fileName,

        int totalRows,

        int processedDays,

        int skippedIncomplete,

        int unmatchedRows,

        CancellationToken cancellationToken = default)

    {

        var phToday = PhilippineTime.Today;



        var upload = new BiometricUpload

        {

            FileName = fileName,

            StartDate = phToday,

            EndDate = phToday,

            TotalRows = totalRows,

            MatchedRows = processedDays,

            UnmatchedRows = unmatchedRows + skippedIncomplete

        };



        _context.BiometricUploads.Add(upload);

        await _context.SaveChangesAsync(cancellationToken);

        return upload.Id;

    }



    public async Task<IReadOnlyDictionary<int, ShiftSchedule>> GetEmployeeSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var defaultSchedule = await LoadDefaultScheduleAsync(cancellationToken);
        var employees = await _context.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.Shift })
            .ToListAsync(cancellationToken);

        return employees.ToDictionary(
            e => e.Id,
            e => e.Shift is null ? defaultSchedule : ToSchedule(e.Shift));
    }

    public async Task<ShiftSchedule> GetScheduleForAttendanceAsync(int attendanceId, CancellationToken cancellationToken = default)
    {
        var shift = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Id == attendanceId)
            .Select(a => a.Employee.Shift)
            .FirstOrDefaultAsync(cancellationToken);

        return shift is null
            ? await LoadDefaultScheduleAsync(cancellationToken)
            : ToSchedule(shift);
    }

    /// <summary>The seeded Day Shift as currently configured; constants only if the row is missing.</summary>
    private async Task<ShiftSchedule> LoadDefaultScheduleAsync(CancellationToken cancellationToken)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ShiftConstants.DefaultShiftId, cancellationToken);

        return shift is null ? ShiftSchedule.Default : ToSchedule(shift);
    }

    private static ShiftSchedule ToSchedule(Shift shift) => new(shift.Name, shift.StartTime, shift.EndTime);

    private static AttendanceDto MapToDto(AttendanceRecord entity, ShiftSchedule defaultSchedule)
    {
        var shift = entity.Employee.Shift is { } assigned ? ToSchedule(assigned) : defaultSchedule;
        var timeIn = entity.TimeIn;
        var timeOut = entity.TimeOut;
        var isNextDayTimeOut = AttendancePolicyProcessor.IsNextDayTimeOut(timeIn, timeOut);
        var hasValidOvertime = AttendancePolicyProcessor.TryGetOvertimeWindow(
            timeIn,
            timeOut,
            isNextDayTimeOut,
            out var overtimeIn,
            out var overtimeOut,
            shift);

        return new AttendanceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeName = $"{entity.Employee.FirstName} {entity.Employee.LastName}",
            EmployeeCode = entity.Employee.EmployeeCode,
            Date = AttendanceDateHelper.ToCalendarDate(entity.Date),
            TimeIn = timeIn,
            TimeOut = timeOut,
            IsLate = AttendanceConstants.IsLateTimeIn(timeIn, shift),
            OvertimeIn = hasValidOvertime ? overtimeIn : null,
            OvertimeOut = hasValidOvertime ? overtimeOut : null,
            IsOvertimeValid = hasValidOvertime,
            IsFromBiometric = entity.IsFromBiometric,
            Notes = entity.Notes,
            ShiftName = shift.Name,
            ShiftStart = shift.Start,
            ShiftEnd = shift.End
        };
    }
}


