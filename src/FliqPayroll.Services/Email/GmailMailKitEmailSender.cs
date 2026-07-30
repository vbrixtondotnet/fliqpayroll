using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Options;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FliqPayroll.Services.Email;

public class GmailMailKitEmailSender : IEmailSender
{
    private readonly GmailOptions _options;
    private readonly IGmailOAuthService _oauthService;
    private readonly ILogger<GmailMailKitEmailSender> _logger;

    public GmailMailKitEmailSender(
        IOptions<GmailOptions> options,
        IGmailOAuthService oauthService,
        ILogger<GmailMailKitEmailSender> logger)
    {
        _options = Guard.AgainstNull(options, nameof(options)).Value;
        _oauthService = Guard.AgainstNull(oauthService, nameof(oauthService));
        _logger = Guard.AgainstNull(logger, nameof(logger));
    }

    public async Task SendAsync(SendEmailRequestDto request, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(request, nameof(request));

        if (string.IsNullOrWhiteSpace(request.To))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(_options.SenderEmail))
        {
            throw new InvalidOperationException("Gmail:SenderEmail is not configured.");
        }

        var accessToken = _options.UsesAppPassword
            ? null
            : await _oauthService.GetAccessTokenAsync(cancellationToken);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("FLIQ Payroll", _options.SenderEmail));
        message.To.Add(MailboxAddress.Parse(request.To.Trim()));
        message.Subject = request.Subject ?? string.Empty;

        var builder = new BodyBuilder
        {
            TextBody = request.BodyText ?? string.Empty
        };

        foreach (var attachment in request.Attachments ?? [])
        {
            if (attachment.Content is null || attachment.Content.Length == 0)
            {
                continue;
            }

            builder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? "application/pdf"
                    : attachment.ContentType));
        }

        message.Body = builder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, cancellationToken);

            if (accessToken is null)
            {
                await client.AuthenticateAsync(
                    _options.SenderEmail,
                    _options.NormalizedAppPassword,
                    cancellationToken);
            }
            else
            {
                await client.AuthenticateAsync(
                    new SaslMechanismOAuth2(_options.SenderEmail, accessToken),
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex, "Gmail SMTP authentication failed for {Sender}.", _options.SenderEmail);
            throw new InvalidOperationException(
                accessToken is null
                    ? "Gmail authentication failed. Verify Gmail:AppPassword is a valid App Password for the sender account."
                    : "Gmail authentication failed. Reconnect via /admin/gmail/connect and try again.",
                ex);
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(ex, "Gmail SMTP command failed when sending to {Recipient}.", request.To);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}.", request.To);
            throw new InvalidOperationException("Failed to send email. Please try again later.", ex);
        }
    }
}
