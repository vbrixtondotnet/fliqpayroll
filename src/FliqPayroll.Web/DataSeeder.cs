using FliqPayroll.Core.Constants;
using FliqPayroll.Core.Enums;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Data;
using FliqPayroll.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FliqPayroll.Web;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FliqPayrollDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FliqPayrollDbContext>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager, logger);
        await SeedUsersAsync(userManager, logger);

        if (await context.Employees.AnyAsync())
        {
            return;
        }

        logger.LogInformation("Seeding initial FliqPayroll data.");

        var employees = new[]
        {
            new Employee
            {
                EmployeeCode = "EMP001",
                FirstName = "Maria",
                LastName = "Santos",
                MiddleName = "L.",
                Gender = Gender.Female,
                CivilStatus = CivilStatus.Single,
                DateOfBirth = new DateTime(1990, 5, 12),
                Nationality = "Filipino",
                MobileNumber = "09171234567",
                Email = "maria.santos@fliqpayroll.local",
                HomeAddress = "123 Rizal St, Makati City",
                EmergencyContactPerson = "Pedro Santos",
                EmergencyContactNumber = "09181234567",
                EmergencyContactRelationship = "Father",
                EmploymentStatus = EmploymentStatus.Regular,
                Department = "Finance",
                Position = "Payroll Specialist",
                Supervisor = "HR Manager",
                HireDate = new DateTime(2022, 3, 15),
                DateRegularized = new DateTime(2022, 9, 15),
                SalaryType = SalaryType.Monthly,
                BasicSalary = 35000m,
                PayrollFrequency = PayrollFrequency.BiMonthly,
                BankName = "BDO",
                BankAccountNumber = "1234567890",
                TinNumber = "123-456-789",
                SssNumber = "34-1234567-8",
                SssErShare = 1750m,
                SssEeShare = 1750m,
                PhilHealthNumber = "12-345678901-2",
                PhilHealthErShare = 875m,
                PhilHealthEeShare = 875m,
                PagIbigNumber = "1212-3456-7890",
                PagIbigErShare = 200m,
                PagIbigEeShare = 200m
            },
            new Employee
            {
                EmployeeCode = "EMP002",
                FirstName = "Juan",
                LastName = "Reyes",
                MiddleName = "D.",
                Gender = Gender.Male,
                CivilStatus = CivilStatus.Married,
                DateOfBirth = new DateTime(1988, 11, 3),
                Nationality = "Filipino",
                MobileNumber = "09191234567",
                Email = "juan.reyes@fliqpayroll.local",
                HomeAddress = "456 Bonifacio Ave, Quezon City",
                EmergencyContactPerson = "Maria Reyes",
                EmergencyContactNumber = "09201234567",
                EmergencyContactRelationship = "Spouse",
                EmploymentStatus = EmploymentStatus.Regular,
                Department = "Operations",
                Position = "Team Lead",
                Supervisor = "Operations Manager",
                HireDate = new DateTime(2021, 8, 1),
                DateRegularized = new DateTime(2022, 2, 1),
                SalaryType = SalaryType.Monthly,
                BasicSalary = 42000m,
                PayrollFrequency = PayrollFrequency.BiMonthly,
                BankName = "BPI",
                BankAccountNumber = "9876543210",
                TinNumber = "987-654-321",
                SssNumber = "34-9876543-2",
                SssErShare = 2100m,
                SssEeShare = 2100m,
                PhilHealthNumber = "98-765432109-8",
                PhilHealthErShare = 1050m,
                PhilHealthEeShare = 1050m,
                PagIbigNumber = "9876-5432-1098",
                PagIbigErShare = 200m,
                PagIbigEeShare = 200m
            },
            new Employee
            {
                EmployeeCode = "EMP003",
                FirstName = "Ana",
                LastName = "Cruz",
                Gender = Gender.Female,
                CivilStatus = CivilStatus.Single,
                DateOfBirth = new DateTime(1995, 2, 20),
                Nationality = "Filipino",
                MobileNumber = "09211234567",
                Email = "ana.cruz@fliqpayroll.local",
                HomeAddress = "789 Mabini St, Pasig City",
                EmploymentStatus = EmploymentStatus.Probationary,
                Department = "HR",
                Position = "HR Officer",
                HireDate = new DateTime(2023, 1, 10),
                SalaryType = SalaryType.Monthly,
                BasicSalary = 30000m,
                PayrollFrequency = PayrollFrequency.BiMonthly,
                BankName = "Metrobank",
                BankAccountNumber = "5555666677",
                TinNumber = "555-666-777",
                SssNumber = "34-5556667-7",
                SssErShare = 1500m,
                SssEeShare = 1500m,
                PhilHealthNumber = "55-566677788-8",
                PhilHealthErShare = 750m,
                PhilHealthEeShare = 750m,
                PagIbigNumber = "5555-6666-7777",
                PagIbigErShare = 200m,
                PagIbigEeShare = 200m
            }
        };

        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();

        var today = PhilippineTime.Today;

        foreach (var employee in employees)
        {
            context.AttendanceRecords.Add(new AttendanceRecord
            {
                EmployeeId = employee.Id,
                Date = today,
                TimeIn = new TimeSpan(8, 0, 0),
                TimeOut = new TimeSpan(17, 0, 0),
                IsLate = false,
                IsOvertimeValid = false,
                IsFromBiometric = false
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        foreach (var role in RoleConstants.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role {Role}.", role);
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@fliqpayroll.local";
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, RoleConstants.SuperAdmin);
            logger.LogInformation("Seeded admin user {Email}.", adminEmail);
        }
        else
        {
            logger.LogWarning("Failed to seed admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
