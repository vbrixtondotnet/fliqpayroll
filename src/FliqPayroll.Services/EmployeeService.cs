using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;

namespace FliqPayroll.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = Guard.AgainstNull(employeeRepository, nameof(employeeRepository));
    }

    public Task<IReadOnlyList<EmployeeDto>> GetAllAsync(EmployeeFilterDto? filter = null, CancellationToken cancellationToken = default) =>
        _employeeRepository.GetAllAsync(filter, cancellationToken);

    public Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _employeeRepository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<string>> GetDepartmentsAsync(CancellationToken cancellationToken = default) =>
        _employeeRepository.GetDepartmentsAsync(cancellationToken);

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(dto, nameof(dto));

        if (dto.BasicSalary < 0)
        {
            throw new ArgumentException("Basic salary cannot be negative.", nameof(dto.BasicSalary));
        }

        return await _employeeRepository.CreateAsync(dto, cancellationToken);
    }

    public async Task<EmployeeDto?> UpdateAsync(UpdateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(dto, nameof(dto));

        if (dto.BasicSalary < 0)
        {
            throw new ArgumentException("Basic salary cannot be negative.", nameof(dto.BasicSalary));
        }

        return await _employeeRepository.UpdateAsync(dto, cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _employeeRepository.DeleteAsync(id, cancellationToken);
}
