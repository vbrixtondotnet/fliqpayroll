using System.Text.Json;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Options;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FliqPayroll.Services.Email;

public class GmailOAuthService : IGmailOAuthService
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string Scope = "https://mail.google.com/";

    private readonly GmailOptions _options;
    private readonly IGmailOAuthTokenStore _tokenStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GmailOAuthService> _logger;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public GmailOAuthService(
        IOptions<GmailOptions> options,
        IGmailOAuthTokenStore tokenStore,
        IHttpClientFactory httpClientFactory,
        ILogger<GmailOAuthService> logger)
    {
        _options = Guard.AgainstNull(options, nameof(options)).Value;
        _tokenStore = Guard.AgainstNull(tokenStore, nameof(tokenStore));
        _httpClientFactory = Guard.AgainstNull(httpClientFactory, nameof(httpClientFactory));
        _logger = Guard.AgainstNull(logger, nameof(logger));
    }

    public string BuildAuthorizationUrl(string? state = null)
    {
        EnsureClientConfigured();

        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            throw new InvalidOperationException("Gmail:RedirectUri is not configured.");
        }

        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = Scope,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true"
        };

        if (!string.IsNullOrWhiteSpace(state))
        {
            query["state"] = state;
        }

        return AuthorizationEndpoint + "?" + string.Join(
            "&",
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    public async Task<GmailOAuthTokenDto> ExchangeCodeAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();

        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            throw new ArgumentException("Authorization code is required.", nameof(authorizationCode));
        }

        var form = new Dictionary<string, string>
        {
            ["code"] = authorizationCode,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["redirect_uri"] = _options.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await PostTokenAsync(form, cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
        {
            throw new InvalidOperationException(
                "Google did not return a refresh token. Revoke prior consent for this app and reconnect with prompt=consent.");
        }

        var token = new GmailOAuthTokenDto
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(tokenResponse.ExpiresIn - 60, 60)),
            Email = _options.SenderEmail
        };

        await _tokenStore.SaveAsync(token, cancellationToken);
        return token;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            var stored = await _tokenStore.LoadAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    "Gmail is not connected. Open /admin/gmail/connect to authorize the sender account.");

            if (!string.IsNullOrWhiteSpace(stored.AccessToken)
                && stored.AccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return stored.AccessToken;
            }

            if (string.IsNullOrWhiteSpace(stored.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Stored Gmail refresh token is missing. Reconnect via /admin/gmail/connect.");
            }

            EnsureClientConfigured();

            var form = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["refresh_token"] = stored.RefreshToken,
                ["grant_type"] = "refresh_token"
            };

            try
            {
                var tokenResponse = await PostTokenAsync(form, cancellationToken);
                stored.AccessToken = tokenResponse.AccessToken;
                stored.AccessTokenExpiresAt =
                    DateTimeOffset.UtcNow.AddSeconds(Math.Max(tokenResponse.ExpiresIn - 60, 60));

                if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
                {
                    stored.RefreshToken = tokenResponse.RefreshToken;
                }

                await _tokenStore.SaveAsync(stored, cancellationToken);
                return stored.AccessToken;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to refresh Gmail OAuth access token.");
                throw new InvalidOperationException(
                    "Gmail access token is invalid or expired. Reconnect via /admin/gmail/connect.",
                    ex);
            }
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public async Task<bool> HasValidTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = await _tokenStore.LoadAsync(cancellationToken);
            return stored is not null && !string.IsNullOrWhiteSpace(stored.RefreshToken);
        }
        catch
        {
            return false;
        }
    }

    private void EnsureClientConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException(
                "Gmail ClientId/ClientSecret are not configured. Set them via User Secrets or environment variables.");
        }
    }

    private async Task<GoogleTokenResponse> PostTokenAsync(
        Dictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(GmailOAuthService));
        using var content = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync(TokenEndpoint, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Google token endpoint returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException("Failed to obtain Gmail OAuth tokens from Google.");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var accessToken = root.GetProperty("access_token").GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Google token response did not include an access token.");
        }

        var expiresIn = root.TryGetProperty("expires_in", out var expiresElement)
            ? expiresElement.GetInt32()
            : 3600;

        var refreshToken = root.TryGetProperty("refresh_token", out var refreshElement)
            ? refreshElement.GetString()
            : null;

        return new GoogleTokenResponse(accessToken, refreshToken, expiresIn);
    }

    private sealed record GoogleTokenResponse(string AccessToken, string? RefreshToken, int ExpiresIn);
}
