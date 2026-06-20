using FliqPayroll.Core.Enums;

namespace FliqPayroll.Core.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public Gender? Gender { get; set; }
    public CivilStatus? CivilStatus { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Nationality { get; set; }
    public string? Religion { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? HomeAddress { get; set; }
    public string? EmergencyContactPerson { get; set; }
    public string? EmergencyContactNumber { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Supervisor { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? DateRegularized { get; set; }
    public SalaryType SalaryType { get; set; }
    public decimal BasicSalary { get; set; }
    public PayrollFrequency PayrollFrequency { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? TinNumber { get; set; }
    public string? SssNumber { get; set; }
    public decimal SssErShare { get; set; }
    public decimal SssEeShare { get; set; }
    public decimal SssLoan { get; set; }
    public string? PhilHealthNumber { get; set; }
    public decimal PhilHealthErShare { get; set; }
    public decimal PhilHealthEeShare { get; set; }
    public string? PagIbigNumber { get; set; }
    public decimal PagIbigErShare { get; set; }
    public decimal PagIbigEeShare { get; set; }
    public decimal PagIbigLoan { get; set; }
    public bool IsActive { get; set; }
    public string FullName => $"{LastName}, {FirstName} {MiddleName}".Trim();
}

public class CreateEmployeeDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public Gender? Gender { get; set; }
    public CivilStatus? CivilStatus { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? Religion { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? HomeAddress { get; set; }
    public string? EmergencyContactPerson { get; set; }
    public string? EmergencyContactNumber { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Regular;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Supervisor { get; set; }
    public DateTime HireDate { get; set; } = DateTime.Today;
    public DateTime? DateRegularized { get; set; }
    public SalaryType SalaryType { get; set; } = SalaryType.Monthly;
    public decimal BasicSalary { get; set; }
    public PayrollFrequency PayrollFrequency { get; set; } = PayrollFrequency.BiMonthly;
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? TinNumber { get; set; }
    public string? SssNumber { get; set; }
    public decimal SssErShare { get; set; }
    public decimal SssEeShare { get; set; }
    public decimal SssLoan { get; set; }
    public string? PhilHealthNumber { get; set; }
    public decimal PhilHealthErShare { get; set; }
    public decimal PhilHealthEeShare { get; set; }
    public string? PagIbigNumber { get; set; }
    public decimal PagIbigErShare { get; set; }
    public decimal PagIbigEeShare { get; set; }
    public decimal PagIbigLoan { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateEmployeeDto : CreateEmployeeDto
{
    public int Id { get; set; }
}

public class EmployeeFilterDto
{
    public string? Search { get; set; }
    public string? Department { get; set; }
    public EmploymentStatus? EmploymentStatus { get; set; }
    public string? Position { get; set; }
    public DateTime? HiredFrom { get; set; }
    public DateTime? HiredTo { get; set; }
    public bool? IsActive { get; set; }
}
