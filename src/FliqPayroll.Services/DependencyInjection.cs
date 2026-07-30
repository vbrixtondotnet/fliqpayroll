using FliqPayroll.Core.Options;
using FliqPayroll.Services.Email;
using FliqPayroll.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FliqPayroll.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddFliqPayrollServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IBiometricService, BiometricService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddScoped<ILeaveService, LeaveService>();

        services.AddHttpClient(nameof(GmailOAuthService));
        services.AddScoped<IGmailOAuthTokenStore, EncryptedFileGmailOAuthTokenStore>();
        services.AddScoped<IGmailOAuthService, GmailOAuthService>();
        services.AddScoped<IEmailSender, GmailMailKitEmailSender>();
        services.AddScoped<IPayslipEmailService, PayslipEmailService>();

        if (configuration is not null)
        {
            services.Configure<GmailOptions>(configuration.GetSection(GmailOptions.SectionName));
        }

        return services;
    }
}
