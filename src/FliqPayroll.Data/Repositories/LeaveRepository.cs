using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FliqPayroll.Data.Repositories;

public class LeaveRepository : ILeaveRepository
{
    private readonly FliqPayrollDbContext _context;

    public LeaveRepository(FliqPayrollDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LeaveDto>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var phStart = PhilippineTime.ToPhilippineDate(startDate);
        var phEnd = PhilippineTime.ToPhilippineDate(endDate);
        var fromStorage = PhilippineTime.ForDateStorage(phStart);
        var toStorage = PhilippineTime.ForDateStorage(phEnd);

        var records = await _context.LeaveRecords
            .AsNoTracking()
            .Include(l => l.Employee)
            .Where(l => l.FromDate <= toStorage && l.ToDate >= fromStorage)
            .OrderBy(l => l.FromDate)
            .ThenBy(l => l.Employee.LastName)
            .ThenBy(l => l.Employee.FirstName)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDto).ToList();
    }

    public async Task<LeaveDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.LeaveRecords
            .AsNoTracking()
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<LeaveDto> CreateAsync(CreateLeaveDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new LeaveRecord
        {
            EmployeeId = dto.EmployeeId,
            FromDate = PhilippineTime.ForDateStorage(dto.FromDate),
            ToDate = PhilippineTime.ForDateStorage(dto.ToDate),
            LeaveType = dto.LeaveType,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRecords.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(entity).Reference(l => l.Employee).LoadAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.LeaveRecords
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _context.LeaveRecords.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken cancellationToken = default) =>
        await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == employeeId, cancellationToken);

    private static LeaveDto MapToDto(LeaveRecord entity) => new()
    {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        EmployeeCode = entity.Employee.EmployeeCode,
        EmployeeName = $"{entity.Employee.LastName}, {entity.Employee.FirstName} {entity.Employee.MiddleName}".Trim(),
        FromDate = PhilippineTime.ToPhilippineDate(entity.FromDate),
        ToDate = PhilippineTime.ToPhilippineDate(entity.ToDate),
        LeaveType = entity.LeaveType,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
