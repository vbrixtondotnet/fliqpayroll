using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FliqPayroll.Web.Jobs;

public class PayslipEmailJobs
{
    private readonly IPayslipEmailService _payslipEmailService;
    private readonly ILogger<PayslipEmailJobs> _logger;

    public PayslipEmailJobs(IPayslipEmailService payslipEmailService, ILogger<PayslipEmailJobs> logger)
    {
        _payslipEmailService = Guard.AgainstNull(payslipEmailService, nameof(payslipEmailService));
        _logger = Guard.AgainstNull(logger, nameof(logger));
    }

    /// <summary>
    /// Hangfire entry point — generates the Employee Copy PDF and emails it.
    /// Do not pass CancellationToken; Hangfire manages job cancellation separately.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    public async Task SendPayslipEmailAsync(int employeeId, int payrollPeriodId)
    {
        _logger.LogInformation(
            "Hangfire: starting payslip email for employee {EmployeeId}, period {PayrollPeriodId}.",
            employeeId,
            payrollPeriodId);

        await _payslipEmailService.SendPayslipEmailAsync(employeeId, payrollPeriodId);

        _logger.LogInformation(
            "Hangfire: completed payslip email for employee {EmployeeId}, period {PayrollPeriodId}.",
            employeeId,
            payrollPeriodId);
    }
}
