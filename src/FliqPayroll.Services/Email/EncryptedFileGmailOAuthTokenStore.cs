using System.Text.Json;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Options;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FliqPayroll.Services.Email;

public class EncryptedFileGmailOAuthTokenStore : IGmailOAuthTokenStore
{
    private const string ProtectorPurpose = "FliqPayroll.GmailOAuthTokens.v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IDataProtector _protector;
    private readonly IHostEnvironment _environment;
    private readonly GmailOptions _options;
    private readonly ILogger<EncryptedFileGmailOAuthTokenStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public EncryptedFileGmailOAuthTokenStore(
        IDataProtectionProvider dataProtectionProvider,
        IHostEnvironment environment,
        IOptions<GmailOptions> options,
        ILogger<EncryptedFileGmailOAuthTokenStore> logger)
    {
        _protector = Guard.AgainstNull(dataProtectionProvider, nameof(dataProtectionProvider))
            .CreateProtector(ProtectorPurpose);
        _environment = Guard.AgainstNull(environment, nameof(environment));
        _options = Guard.AgainstNull(options, nameof(options)).Value;
        _logger = Guard.AgainstNull(logger, nameof(logger));
    }

    public async Task<GmailOAuthTokenDto?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = ResolveStorePath();
            if (!File.Exists(path))
            {
                return null;
            }

            var encrypted = await File.ReadAllTextAsync(path, cancellationToken);
            if (string.IsNullOrWhiteSpace(encrypted))
            {
                return null;
            }

            try
            {
                var json = _protector.Unprotect(encrypted);
                return JsonSerializer.Deserialize<GmailOAuthTokenDto>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt Gmail OAuth token store at {Path}.", path);
                throw new InvalidOperationException(
                    "Stored Gmail OAuth tokens are invalid or could not be decrypted. Reconnect via /admin/gmail/connect.",
                    ex);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(GmailOAuthTokenDto token, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(token, nameof(token));
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(token));
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = ResolveStorePath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(token, JsonOptions);
            var encrypted = _protector.Protect(json);
            await File.WriteAllTextAsync(path, encrypted, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = ResolveStorePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private string ResolveStorePath()
    {
        var configured = string.IsNullOrWhiteSpace(_options.TokenStorePath)
            ? "App_Data/gmail-oauth.tokens"
            : _options.TokenStorePath.Trim();

        return Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configured));
    }
}
