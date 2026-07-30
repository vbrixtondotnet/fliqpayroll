using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FliqPayroll.Services.Email;

public class PayslipEmailService : IPayslipEmailService
{
    private readonly IReportService _reportService;
    private readonly IPayslipDocumentGenerator _documentGenerator;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PayslipEmailService> _logger;

    public PayslipEmailService(
        IReportService reportService,
        IPayslipDocumentGenerator documentGenerator,
        IEmailSender emailSender,
        ILogger<PayslipEmailService> logger)
    {
        _reportService = Guard.AgainstNull(reportService, nameof(reportService));
        _documentGenerator = Guard.AgainstNull(documentGenerator, nameof(documentGenerator));
        _emailSender = Guard.AgainstNull(emailSender, nameof(emailSender));
        _logger = Guard.AgainstNull(logger, nameof(logger));
    }

    public async Task SendPayslipEmailAsync(
        int employeeId,
        int payrollPeriodId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentException("Employee id is required.", nameof(employeeId));
        }

        if (payrollPeriodId <= 0)
        {
            throw new ArgumentException("Payroll period id is required.", nameof(payrollPeriodId));
        }

        var payslip = await _reportService.GetPayslipByPeriodIdAsync(employeeId, payrollPeriodId, cancellationToken);
        if (payslip is null)
        {
            throw new InvalidOperationException("Payslip not found for the selected employee and payroll period.");
        }

        var email = payslip.Employee.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Missing Email Address");
        }

        var payrollPeriod = _documentGenerator.FormatPayrollPeriod(
            payslip.Period.StartDate,
            payslip.Period.EndDate);

        if (string.IsNullOrWhiteSpace(payrollPeriod))
        {
            payrollPeriod = payslip.Period.Name;
        }

        var firstName = string.IsNullOrWhiteSpace(payslip.Employee.FirstName)
            ? "Employee"
            : payslip.Employee.FirstName.Trim();

        byte[] pdf;
        try
        {
            pdf = _documentGenerator.GenerateEmployeeCopy(payslip);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate payslip PDF for employee {EmployeeId}.", employeeId);
            throw new InvalidOperationException("Failed to generate the payslip PDF.", ex);
        }

        var fileName = _documentGenerator.BuildSinglePayslipFileName(payslip);
        var subject = $"Payslip for the period {payrollPeriod}";
        var body =
            $"Hello {firstName},\n\n" +
            $"Attached is your payslip for the period {payrollPeriod}.\n";

        await _emailSender.SendAsync(
            new SendEmailRequestDto
            {
                To = email,
                Subject = subject,
                BodyText = body,
                Attachments =
                [
                    new EmailAttachmentDto
                    {
                        FileName = fileName,
                        ContentType = "application/pdf",
                        Content = pdf
                    }
                ]
            },
            cancellationToken);

        _logger.LogInformation(
            "Payslip emailed to {Email} for employee {EmployeeId}, period {PayrollPeriodId}.",
            email,
            employeeId,
            payrollPeriodId);
    }
}
