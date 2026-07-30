using FliqPayroll.Core.DTOs;

namespace FliqPayroll.Services.Interfaces;

public interface IGmailOAuthTokenStore
{
    Task<GmailOAuthTokenDto?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(GmailOAuthTokenDto token, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

public interface IGmailOAuthService
{
    string BuildAuthorizationUrl(string? state = null);
    Task<GmailOAuthTokenDto> ExchangeCodeAsync(string authorizationCode, CancellationToken cancellationToken = default);
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<bool> HasValidTokensAsync(CancellationToken cancellationToken = default);
}

public interface IEmailSender
{
    Task SendAsync(SendEmailRequestDto request, CancellationToken cancellationToken = default);
}

public interface IPayslipEmailService
{
    /// <summary>
    /// Generates the Employee Copy PDF and emails it to the employee's address.
    /// </summary>
    Task SendPayslipEmailAsync(int employeeId, int payrollPeriodId, CancellationToken cancellationToken = default);
}
