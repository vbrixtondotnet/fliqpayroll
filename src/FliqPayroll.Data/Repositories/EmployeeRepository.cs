using FliqPayroll.Core.DTOs;

using FliqPayroll.Core.Interfaces;

using FliqPayroll.Core.Utilities;

using FliqPayroll.Data.Entities;

using Microsoft.EntityFrameworkCore;



namespace FliqPayroll.Data.Repositories;



public class EmployeeRepository : IEmployeeRepository

{

    private readonly FliqPayrollDbContext _context;



    public EmployeeRepository(FliqPayrollDbContext context)

    {

        _context = Guard.AgainstNull(context, nameof(context));

    }



    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(EmployeeFilterDto? filter = null, CancellationToken cancellationToken = default)

    {

        var query = _context.Employees.AsNoTracking();



        if (filter is not null)

        {

            if (!string.IsNullOrWhiteSpace(filter.Search))

            {

                var search = filter.Search.Trim();

                query = query.Where(e =>

                    e.EmployeeCode.Contains(search) ||

                    e.FirstName.Contains(search) ||

                    e.LastName.Contains(search) ||

                    (e.MiddleName != null && e.MiddleName.Contains(search)) ||

                    (e.Email != null && e.Email.Contains(search)));

            }



            if (!string.IsNullOrWhiteSpace(filter.Department))

            {

                var department = filter.Department.Trim();

                query = query.Where(e => e.Department == department);

            }



            if (filter.EmploymentStatus.HasValue)

            {

                query = query.Where(e => e.EmploymentStatus == filter.EmploymentStatus.Value);

            }



            if (!string.IsNullOrWhiteSpace(filter.Position))

            {

                var position = filter.Position.Trim();

                query = query.Where(e => e.Position == position);

            }



            if (filter.HiredFrom.HasValue)

            {

                var hiredFrom = filter.HiredFrom.Value.Date;

                query = query.Where(e => e.HireDate >= hiredFrom);

            }



            if (filter.HiredTo.HasValue)

            {

                var hiredTo = filter.HiredTo.Value.Date;

                query = query.Where(e => e.HireDate <= hiredTo);

            }



            if (filter.IsActive.HasValue)

            {

                query = query.Where(e => e.IsActive == filter.IsActive.Value);

            }

        }



        var entities = await query

            .OrderBy(e => e.LastName)

            .ThenBy(e => e.FirstName)

            .ToListAsync(cancellationToken);



        return entities.Select(MapToDto).ToList();

    }



    public async Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)

    {

        var entity = await _context.Employees

            .AsNoTracking()

            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);



        return entity is null ? null : MapToDto(entity);

    }



    public async Task<EmployeeDto?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default)

    {

        var code = Guard.AgainstNullOrWhiteSpace(employeeCode, nameof(employeeCode));



        var entity = await _context.Employees

            .AsNoTracking()

            .FirstOrDefaultAsync(e => e.EmployeeCode == code, cancellationToken);



        return entity is null ? null : MapToDto(entity);

    }

    public async Task<IReadOnlyDictionary<string, int>> GetEmployeeIdsByCodesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Select(e => new { e.EmployeeCode, e.Id })
            .ToListAsync(cancellationToken);

        return employees.ToDictionary(
            e => e.EmployeeCode.Trim(),
            e => e.Id,
            StringComparer.OrdinalIgnoreCase);
    }



    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default)

    {

        Guard.AgainstNull(dto, nameof(dto));



        var entity = MapToEntity(new Employee(), dto);

        _context.Employees.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);



        return MapToDto(entity);

    }



    public async Task<EmployeeDto?> UpdateAsync(UpdateEmployeeDto dto, CancellationToken cancellationToken = default)

    {

        Guard.AgainstNull(dto, nameof(dto));



        var entity = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id, cancellationToken);

        if (entity is null)

        {

            return null;

        }



        MapToEntity(entity, dto);

        entity.UpdatedAt = DateTime.UtcNow;



        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);

    }



    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)

    {

        var entity = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)

        {

            return false;

        }



        _context.Employees.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return true;

    }



    public Task<int> CountActiveAsync(CancellationToken cancellationToken = default) =>

        _context.Employees.CountAsync(e => e.IsActive, cancellationToken);



    public Task<int> CountAllAsync(CancellationToken cancellationToken = default) =>

        _context.Employees.CountAsync(cancellationToken);



    public async Task<IReadOnlyList<string>> GetDepartmentsAsync(CancellationToken cancellationToken = default)

    {

        return await _context.Employees

            .AsNoTracking()

            .Where(e => e.Department != null && e.Department != string.Empty)

            .Select(e => e.Department!)

            .Distinct()

            .OrderBy(d => d)

            .ToListAsync(cancellationToken);

    }



    private static Employee MapToEntity(Employee entity, CreateEmployeeDto dto)

    {

        entity.EmployeeCode = Guard.AgainstNullOrWhiteSpace(dto.EmployeeCode, nameof(dto.EmployeeCode));

        entity.LastName = Guard.AgainstNullOrWhiteSpace(dto.LastName, nameof(dto.LastName));

        entity.FirstName = Guard.AgainstNullOrWhiteSpace(dto.FirstName, nameof(dto.FirstName));

        entity.MiddleName = dto.MiddleName?.Trim();

        entity.Gender = dto.Gender;

        entity.CivilStatus = dto.CivilStatus;

        entity.DateOfBirth = dto.DateOfBirth?.Date;

        entity.Nationality = dto.Nationality?.Trim();

        entity.Religion = dto.Religion?.Trim();

        entity.MobileNumber = dto.MobileNumber?.Trim();

        entity.Email = dto.Email?.Trim();

        entity.HomeAddress = dto.HomeAddress?.Trim();

        entity.EmergencyContactPerson = dto.EmergencyContactPerson?.Trim();

        entity.EmergencyContactNumber = dto.EmergencyContactNumber?.Trim();

        entity.EmergencyContactRelationship = dto.EmergencyContactRelationship?.Trim();

        entity.EmploymentStatus = dto.EmploymentStatus;

        entity.Department = dto.Department?.Trim();

        entity.Position = dto.Position?.Trim();

        entity.Supervisor = dto.Supervisor?.Trim();

        entity.HireDate = dto.HireDate.Date;

        entity.DateRegularized = dto.DateRegularized?.Date;

        entity.SalaryType = dto.SalaryType;

        entity.BasicSalary = dto.BasicSalary;

        entity.PayrollFrequency = dto.PayrollFrequency;

        entity.BankName = dto.BankName?.Trim();

        entity.BankAccountNumber = dto.BankAccountNumber?.Trim();

        entity.TinNumber = dto.TinNumber?.Trim();

        entity.SssNumber = dto.SssNumber?.Trim();

        entity.SssErShare = dto.SssErShare;

        entity.SssEeShare = dto.SssEeShare;

        entity.SssLoan = dto.SssLoan;

        entity.PhilHealthNumber = dto.PhilHealthNumber?.Trim();

        entity.PhilHealthErShare = dto.PhilHealthErShare;

        entity.PhilHealthEeShare = dto.PhilHealthEeShare;

        entity.PagIbigNumber = dto.PagIbigNumber?.Trim();

        entity.PagIbigErShare = dto.PagIbigErShare;

        entity.PagIbigEeShare = dto.PagIbigEeShare;

        entity.PagIbigLoan = dto.PagIbigLoan;

        entity.IsActive = dto.IsActive;

        return entity;

    }



    private static EmployeeDto MapToDto(Employee entity) => new()

    {

        Id = entity.Id,

        EmployeeCode = entity.EmployeeCode,

        LastName = entity.LastName,

        FirstName = entity.FirstName,

        MiddleName = entity.MiddleName,

        Gender = entity.Gender,

        CivilStatus = entity.CivilStatus,

        DateOfBirth = entity.DateOfBirth,

        Age = entity.DateOfBirth.HasValue

            ? (int)((PhilippineTime.Today - PhilippineTime.ToPhilippineDate(entity.DateOfBirth.Value)).TotalDays / 365.25)

            : null,

        Nationality = entity.Nationality,

        Religion = entity.Religion,

        MobileNumber = entity.MobileNumber,

        Email = entity.Email,

        HomeAddress = entity.HomeAddress,

        EmergencyContactPerson = entity.EmergencyContactPerson,

        EmergencyContactNumber = entity.EmergencyContactNumber,

        EmergencyContactRelationship = entity.EmergencyContactRelationship,

        EmploymentStatus = entity.EmploymentStatus,

        Department = entity.Department,

        Position = entity.Position,

        Supervisor = entity.Supervisor,

        HireDate = entity.HireDate,

        DateRegularized = entity.DateRegularized,

        SalaryType = entity.SalaryType,

        BasicSalary = entity.BasicSalary,

        PayrollFrequency = entity.PayrollFrequency,

        BankName = entity.BankName,

        BankAccountNumber = entity.BankAccountNumber,

        TinNumber = entity.TinNumber,

        SssNumber = entity.SssNumber,

        SssErShare = entity.SssErShare,

        SssEeShare = entity.SssEeShare,

        SssLoan = entity.SssLoan,

        PhilHealthNumber = entity.PhilHealthNumber,

        PhilHealthErShare = entity.PhilHealthErShare,

        PhilHealthEeShare = entity.PhilHealthEeShare,

        PagIbigNumber = entity.PagIbigNumber,

        PagIbigErShare = entity.PagIbigErShare,

        PagIbigEeShare = entity.PagIbigEeShare,

        PagIbigLoan = entity.PagIbigLoan,

        IsActive = entity.IsActive

    };

}


