using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FliqPayroll.Data.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly FliqPayrollDbContext _context;

    public ShiftRepository(FliqPayrollDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ShiftDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var shifts = await _context.Shifts
            .AsNoTracking()
            .Include(s => s.Employees.Where(e => e.IsActive))
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        return shifts.Select(MapToDto).ToList();
    }

    public async Task<ShiftDto?> GetByIdAsync(int shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .Include(s => s.Employees.Where(e => e.IsActive))
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

        return shift is null ? null : MapToDto(shift);
    }

    public async Task<ShiftDto?> UpdateAsync(int shiftId, string name, TimeSpan startTime, TimeSpan endTime, CancellationToken cancellationToken = default)
    {
        var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);
        if (shift is null)
        {
            return null;
        }

        shift.Name = name;
        shift.StartTime = startTime;
        shift.EndTime = endTime;
        shift.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(shiftId, cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, int excludeShiftId, CancellationToken cancellationToken = default) =>
        _context.Shifts.AnyAsync(s => s.Id != excludeShiftId && s.Name == name, cancellationToken);

    public async Task<ShiftDto?> SetEmployeesAsync(int shiftId, IReadOnlyCollection<int> employeeIds, CancellationToken cancellationToken = default)
    {
        if (!await _context.Shifts.AnyAsync(s => s.Id == shiftId, cancellationToken))
        {
            return null;
        }

        var wanted = employeeIds.ToHashSet();
        var now = DateTime.UtcNow;

        var affected = await _context.Employees
            .Where(e => e.ShiftId == shiftId || wanted.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var employee in affected)
        {
            var shouldBeInShift = wanted.Contains(employee.Id);
            if (shouldBeInShift && employee.ShiftId != shiftId)
            {
                employee.ShiftId = shiftId;
                employee.UpdatedAt = now;
            }
            else if (!shouldBeInShift && employee.ShiftId == shiftId)
            {
                employee.ShiftId = null;
                employee.UpdatedAt = now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(shiftId, cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftEmployeeDto>> GetAssignableEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToListAsync(cancellationToken);

        return employees.Select(MapEmployee).ToList();
    }

    private static ShiftDto MapToDto(Shift shift) => new()
    {
        Id = shift.Id,
        Name = shift.Name,
        StartTime = shift.StartTime.ToString(@"hh\:mm"),
        EndTime = shift.EndTime.ToString(@"hh\:mm"),
        Employees = shift.Employees
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(MapEmployee)
            .ToList()
    };

    private static ShiftEmployeeDto MapEmployee(Employee e) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FullName = $"{e.LastName}, {e.FirstName}",
        Department = e.Department,
        Position = e.Position,
        ShiftId = e.ShiftId
    };
}
