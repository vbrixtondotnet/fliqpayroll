using FliqPayroll.Core.Interfaces;
using FliqPayroll.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FliqPayroll.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddFliqPayrollData(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<FliqPayrollDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IPayrollPeriodRepository, PayrollPeriodRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }
}
