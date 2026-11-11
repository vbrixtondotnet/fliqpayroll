using System.Globalization;
using System.Net;
using System.Text;
using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Data.Entities;
using FliqPayroll.Services.Interfaces;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.Identity;

namespace FliqPayroll.Web.Jobs;

/// <summary>
/// Emails every active Finance Officer one consolidated report of the pending payroll changes of a period,
/// with a link to the approvals page. Scheduled by the first pending change of a period; later changes do not
/// schedule another job until the period has been fully reviewed (see PayrollPeriod.ApprovalNotifiedAt).
/// </summary>
public class PayrollApprovalEmailJobs
{
    private const int MaxAttempts = 3;

    private readonly IPayrollService _payrollService;
    private readonly IPayrollPeriodRepository _payrollPeriodRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PayrollApprovalEmailJobs> _logger;

    public PayrollApprovalEmailJobs(
        IPayrollService payrollService,
        IPayrollPeriodRepository payrollPeriodRepository,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<PayrollApprovalEmailJobs> logger)
    {
        _payrollService = Guard.AgainstNull(payrollService, nameof(payrollService));
        _payrollPeriodRepository = Guard.AgainstNull(payrollPeriodRepository, nameof(payrollPeriodRepository));
        _userManager = Guard.AgainstNull(userManager, nameof(userManager));
        _emailSender = Guard.AgainstNull(emailSender, nameof(emailSender));
        _logger = Guard.AgainstNull(logger, nameof(logger));
    }

    /// <param name="baseUrl">Site root (e.g. https://host) captured from the request that triggered the job.</param>
    /// <param name="context">Supplied by Hangfire; pass null when enqueuing.</param>
    [AutomaticRetry(Attempts = MaxAttempts - 1, DelaysInSeconds = [60, 300])]
    public async Task SendAsync(int payrollPeriodId, string baseUrl, PerformContext? context)
    {
        try
        {
            await SendCoreAsync(payrollPeriodId, baseUrl);
        }
        catch (Exception ex)
        {
            var retryCount = context?.GetJobParameter<int>("RetryCount") ?? 0;
            if (retryCount >= MaxAttempts - 1)
            {
                // Out of retries: free the period so the next change tries to notify again.
                _logger.LogError(ex, "Giving up emailing approvers for payroll period {PeriodId}.", payrollPeriodId);
                await _payrollPeriodRepository.ResetApprovalNotificationAsync(payrollPeriodId);
            }

            throw;
        }
    }

    private async Task SendCoreAsync(int payrollPeriodId, string baseUrl)
    {
        var pending = await _payrollService.GetChangeRequestsAsync(payrollPeriodId, pendingOnly: true);
        if (pending.Count == 0)
        {
            // Everything was reverted or reviewed during the delay; nothing to ask for.
            await _payrollPeriodRepository.ResetApprovalNotificationAsync(payrollPeriodId);
            _logger.LogInformation("No pending changes left for period {PeriodId}; approval email skipped.", payrollPeriodId);
            return;
        }

        var recipients = (await _userManager.GetUsersInRoleAsync(RoleConstants.FinanceOfficer))
            .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => u.Email!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            await _payrollPeriodRepository.ResetApprovalNotificationAsync(payrollPeriodId);
            _logger.LogWarning(
                "Payroll period {PeriodId} has pending changes but no active {Role} user has an email address.",
                payrollPeriodId,
                RoleConstants.FinanceOfficer);
            return;
        }

        var periodName = pending[0].PeriodName;
        var link = $"{baseUrl.TrimEnd('/')}/Approvals/Period/{payrollPeriodId}";
        var ordered = pending.OrderBy(p => p.EmployeeName, StringComparer.OrdinalIgnoreCase).ToList();
        var employeeCount = ordered.Select(p => p.EmployeeId).Distinct().Count();

        var request = new SendEmailRequestDto
        {
            Subject = $"Payroll approval needed: {periodName} ({employeeCount} employee{(employeeCount == 1 ? "" : "s")})",
            BodyText = BuildText(periodName, link, ordered),
            BodyHtml = BuildHtml(periodName, link, ordered)
        };

        var failures = 0;
        foreach (var recipient in recipients)
        {
            try
            {
                await _emailSender.SendAsync(new SendEmailRequestDto
                {
                    To = recipient,
                    Subject = request.Subject,
                    BodyText = request.BodyText,
                    BodyHtml = request.BodyHtml
                });
            }
            catch (Exception ex)
            {
                failures++;
                _logger.LogError(ex, "Failed to email {Recipient} about period {PeriodId}.", recipient, payrollPeriodId);
            }
        }

        if (failures == recipients.Count)
        {
            // Nobody received it: fail the job so Hangfire retries.
            throw new InvalidOperationException("The approval email could not be sent to any Finance Officer.");
        }

        // Partial failures are not retried: that would email the people who already got it a second time.
        _logger.LogInformation(
            "Approval email for period {PeriodId} sent to {Sent}/{Total} Finance Officer(s).",
            payrollPeriodId,
            recipients.Count - failures,
            recipients.Count);
    }

    private static string Format(PayrollFieldChangeDto change, string? value) =>
        change.Field == "PaymentMethod"
            ? value ?? string.Empty
            : decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
                ? number.ToString("N2", CultureInfo.InvariantCulture)
                : value ?? string.Empty;

    private static string Local(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), PhilippineTimeZone())
            .ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);

    private static TimeZoneInfo PhilippineTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
        }
    }

    private static string BuildText(string periodName, string link, IReadOnlyList<PayrollChangeRequestDto> requests)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Payroll changes are waiting for your approval for the period {periodName}.");
        sb.AppendLine();

        foreach (var request in requests)
        {
            sb.AppendLine($"{request.EmployeeCode} - {request.EmployeeName} (edited by {request.RequestedBy}, {Local(request.RequestedAt)})");
            foreach (var change in request.Changes)
            {
                sb.AppendLine($"  {change.Label}: {Format(change, change.OldValue)} -> {Format(change, change.NewValue)}");
            }

            sb.AppendLine();
        }

        sb.AppendLine($"Review and approve: {link}");
        return sb.ToString();
    }

    private static string BuildHtml(string periodName, string link, IReadOnlyList<PayrollChangeRequestDto> requests)
    {
        string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var fieldCount = requests.Sum(r => r.Changes.Count);
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2937;max-width:720px\">");
        sb.Append("<h2 style=\"margin:0 0 4px\">Payroll approval needed</h2>");
        sb.Append($"<p style=\"margin:0 0 16px;color:#6b7280\">Period: <strong style=\"color:#1f2937\">{E(periodName)}</strong></p>");
        sb.Append($"<p>{requests.Count} employee row{(requests.Count == 1 ? "" : "s")} with {fieldCount} changed field{(fieldCount == 1 ? "" : "s")} " +
                  "are waiting for a Finance Officer to approve or reject them. Payslips for these employees are on hold until then.</p>");
        sb.Append($"<p style=\"margin:20px 0\"><a href=\"{E(link)}\" style=\"background:#0d6efd;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none;font-weight:600\">Review and approve changes</a></p>");

        foreach (var request in requests)
        {
            sb.Append("<table style=\"border-collapse:collapse;width:100%;margin:0 0 18px;font-size:13px\">");
            sb.Append($"<tr><td colspan=\"3\" style=\"background:#ffe8cc;padding:8px 10px;border:1px solid #f3c58f\">" +
                      $"<strong>{E(request.EmployeeCode)} - {E(request.EmployeeName)}</strong>" +
                      $"<span style=\"color:#6b7280\"> &nbsp;edited by {E(request.RequestedBy)} on {E(Local(request.RequestedAt))}</span></td></tr>");
            sb.Append("<tr style=\"background:#f3f4f6\"><th align=\"left\" style=\"padding:6px 10px;border:1px solid #e5e7eb\">Field</th>" +
                      "<th align=\"right\" style=\"padding:6px 10px;border:1px solid #e5e7eb\">Original</th>" +
                      "<th align=\"right\" style=\"padding:6px 10px;border:1px solid #e5e7eb\">Revised</th></tr>");

            foreach (var change in request.Changes)
            {
                sb.Append($"<tr><td style=\"padding:6px 10px;border:1px solid #e5e7eb\">{E(change.Label)}</td>" +
                          $"<td align=\"right\" style=\"padding:6px 10px;border:1px solid #e5e7eb;color:#6b7280\">{E(Format(change, change.OldValue))}</td>" +
                          $"<td align=\"right\" style=\"padding:6px 10px;border:1px solid #e5e7eb;font-weight:600\">{E(Format(change, change.NewValue))}</td></tr>");
            }

            sb.Append("</table>");
        }

        sb.Append($"<p style=\"color:#6b7280;font-size:12px\">If the button does not work, open this link: <a href=\"{E(link)}\">{E(link)}</a><br />" +
                  "You will not receive another email for this period until all of these changes have been reviewed.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }
}
