using FliqPayroll.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FliqPayroll.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddFliqPayrollServices(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IBiometricService, BiometricService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddScoped<ILeaveService, LeaveService>();

        return services;
    }
}
