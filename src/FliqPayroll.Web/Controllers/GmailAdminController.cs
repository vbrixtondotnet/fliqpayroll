using System.Collections.Concurrent;
using FliqPayroll.Core.Constants;
using FliqPayroll.Core.Options;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FliqPayroll.Web.Controllers;

[Authorize(Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.HrAdmin}")]
[Route("admin/gmail")]
public class GmailAdminController : Controller
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> PendingStates = new();

    private readonly IGmailOAuthService _gmailOAuthService;
    private readonly GmailOptions _options;
    private readonly ILogger<GmailAdminController> _logger;

    public GmailAdminController(
        IGmailOAuthService gmailOAuthService,
        IOptions<GmailOptions> options,
        ILogger<GmailAdminController> logger)
    {
        _gmailOAuthService = gmailOAuthService;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("connect")]
    public IActionResult Connect()
    {
        if (_options.UsesAppPassword)
        {
            return Content(
                "Gmail:AppPassword is configured, so payslip email uses App Password authentication. "
                    + "Clear Gmail:AppPassword to switch back to OAuth.",
                "text/plain");
        }

        try
        {
            CleanupExpiredStates();
            var state = Guid.NewGuid().ToString("N");
            PendingStates[state] = DateTimeOffset.UtcNow.AddMinutes(15);
            var url = _gmailOAuthService.BuildAuthorizationUrl(state);
            return Redirect(url);
        }
        catch (InvalidOperationException ex)
        {
            return Content(ex.Message, "text/plain");
        }
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Content($"Gmail authorization failed: {error}", "text/plain");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Missing authorization code.");
        }

        if (string.IsNullOrWhiteSpace(state)
            || !PendingStates.TryRemove(state, out var expiresAt)
            || expiresAt < DateTimeOffset.UtcNow)
        {
            return BadRequest("Invalid or expired OAuth state. Start again from /admin/gmail/connect.");
        }

        try
        {
            await _gmailOAuthService.ExchangeCodeAsync(code, cancellationToken);
            return Content(
                "Gmail connected successfully. You can close this window and email payslips from the Payslips page.",
                "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Gmail OAuth callback.");
            return Content($"Failed to connect Gmail: {ex.Message}", "text/plain");
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        if (_options.UsesAppPassword)
        {
            return Json(new { Mode = "AppPassword", Connected = true });
        }

        var connected = await _gmailOAuthService.HasValidTokensAsync(cancellationToken);
        return Json(new { Mode = "OAuth", Connected = connected });
    }

    private static void CleanupExpiredStates()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in PendingStates)
        {
            if (pair.Value < now)
            {
                PendingStates.TryRemove(pair.Key, out _);
            }
        }
    }
}
