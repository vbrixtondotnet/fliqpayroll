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

            .Where(a => a.Date >= phStart && a.Date < endExclusive)

            .OrderBy(a => a.Employee.LastName)

            .ThenBy(a => a.Employee.FirstName)

            .ThenBy(a => a.Date)

            .ToListAsync(cancellationToken);



        return entities.Select(MapToDto).ToList();

    }



    public async Task<IReadOnlyList<AttendanceDto>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)

    {

        var calendarDate = AttendanceDateHelper.ToCalendarDate(date);



        var entities = await _context.AttendanceRecords

            .AsNoTracking()

            .Include(a => a.Employee)

            .Where(a =>

                a.Date.Year == calendarDate.Year &&

                a.Date.Month == calendarDate.Month &&

                a.Date.Day == calendarDate.Day)

            .OrderBy(a => a.Employee.LastName)

            .ThenBy(a => a.Employee.FirstName)

            .ToListAsync(cancellationToken);



        return entities.Select(MapToDto).ToList();

    }



    public async Task<AttendanceDto?> UpdateAsync(UpdateAttendanceDto dto, CancellationToken cancellationToken = default)

    {

        Guard.AgainstNull(dto, nameof(dto));



        var entity = await _context.AttendanceRecords

            .Include(a => a.Employee)

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

        return MapToDto(entity);

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

            .Where(a =>

                a.EmployeeId == employeeId &&

                a.Date >= phStart &&

                a.Date < endExclusive)

            .OrderBy(a => a.Date)

            .ToListAsync(cancellationToken);



        return entities.Select(MapToDto).ToList();

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



    private static AttendanceDto MapToDto(AttendanceRecord entity) => new()

    {

        Id = entity.Id,

        EmployeeId = entity.EmployeeId,

        EmployeeName = $"{entity.Employee.FirstName} {entity.Employee.LastName}",

        EmployeeCode = entity.Employee.EmployeeCode,

        Date = AttendanceDateHelper.ToCalendarDate(entity.Date),

        TimeIn = entity.TimeIn,

        TimeOut = entity.TimeOut,

        IsLate = entity.IsLate,

        OvertimeIn = entity.OvertimeIn,

        OvertimeOut = entity.OvertimeOut,

        IsOvertimeValid = entity.IsOvertimeValid,

        IsFromBiometric = entity.IsFromBiometric,

        Notes = entity.Notes

    };

}


