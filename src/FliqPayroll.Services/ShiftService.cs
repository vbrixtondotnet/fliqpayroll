using System.Globalization;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;

namespace FliqPayroll.Services;

public class ShiftService : IShiftService
{
    private readonly IShiftRepository _shiftRepository;

    public ShiftService(IShiftRepository shiftRepository)
    {
        _shiftRepository = Guard.AgainstNull(shiftRepository, nameof(shiftRepository));
    }

    public Task<IReadOnlyList<ShiftDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _shiftRepository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<ShiftEmployeeDto>> GetAssignableEmployeesAsync(CancellationToken cancellationToken = default) =>
        _shiftRepository.GetAssignableEmployeesAsync(cancellationToken);

    public async Task<ShiftDto?> UpdateAsync(int shiftId, UpdateShiftDto dto, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(dto, nameof(dto));

        var name = (dto.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Shift name is required.");
        }

        if (name.Length > 50)
        {
            throw new ArgumentException("Shift name must be 50 characters or fewer.");
        }

        var start = ParseTime(dto.StartTime, "Start time");
        var end = ParseTime(dto.EndTime, "End time");
        if (start == end)
        {
            throw new ArgumentException("Start time and end time cannot be the same.");
        }

        if (await _shiftRepository.NameExistsAsync(name, shiftId, cancellationToken))
        {
            throw new InvalidOperationException("Another shift already uses this name.");
        }

        return await _shiftRepository.UpdateAsync(shiftId, name, start, end, cancellationToken);
    }

    public Task<ShiftDto?> SetEmployeesAsync(int shiftId, AssignShiftEmployeesDto dto, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(dto, nameof(dto));
        return _shiftRepository.SetEmployeesAsync(shiftId, (dto.EmployeeIds ?? new()).Distinct().ToList(), cancellationToken);
    }

    private static TimeSpan ParseTime(string? value, string label)
    {
        if (!TimeSpan.TryParseExact(value?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var time))
        {
            throw new ArgumentException($"{label} must be in HH:mm format.");
        }

        return time;
    }
}
