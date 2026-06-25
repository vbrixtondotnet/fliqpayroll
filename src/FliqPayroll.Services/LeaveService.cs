using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Enums;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;

namespace FliqPayroll.Services;

public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;

    public LeaveService(ILeaveRepository leaveRepository)
    {
        _leaveRepository = Guard.AgainstNull(leaveRepository, nameof(leaveRepository));
    }

    public Task<IReadOnlyList<LeaveDto>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default) =>
        _leaveRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

    public async Task<LeaveDto> CreateAsync(CreateLeaveDto dto, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(dto, nameof(dto));

        if (dto.EmployeeId <= 0)
        {
            throw new ArgumentException("Employee is required.");
        }

        if (!await _leaveRepository.EmployeeExistsAsync(dto.EmployeeId, cancellationToken))
        {
            throw new ArgumentException("Employee not found.");
        }

        var fromDate = PhilippineTime.ToPhilippineDate(dto.FromDate);
        var toDate = PhilippineTime.ToPhilippineDate(dto.ToDate);

        if (toDate < fromDate)
        {
            throw new ArgumentException("To date must be on or after From date.");
        }

        if (!Enum.IsDefined(typeof(LeaveType), dto.LeaveType))
        {
            throw new ArgumentException("Invalid leave type.");
        }

        return await _leaveRepository.CreateAsync(
            new CreateLeaveDto
            {
                EmployeeId = dto.EmployeeId,
                FromDate = fromDate,
                ToDate = toDate,
                LeaveType = dto.LeaveType
            },
            cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _leaveRepository.DeleteAsync(id, cancellationToken);
}
