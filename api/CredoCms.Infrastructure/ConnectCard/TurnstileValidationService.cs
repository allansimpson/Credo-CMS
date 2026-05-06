using System.Net.Http.Json;
using CredoCms.Application.ConnectCard;
using CredoCms.Application.SiteSettingsManagement;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.ConnectCard;

/// <summary>
/// Cloudflare Turnstile validator. POSTs to <c>https://challenges.cloudflare.com
/// /turnstile/v0/siteverify</c> with <c>secret</c> + <c>response</c> + optional
/// <c>remoteip</c>; returns <c>true</c> when the response's <c>success</c> field
/// is true. When the secret key isn't configured (e.g. local dev), the service
/// short-circuits to <c>true</c> so the connect card form remains testable
/// without a real Turnstile site key.
/// </summary>
public sealed class TurnstileValidationService : ITurnstileValidationService
{
    public const string HttpClientName = "Cloudflare.Turnstile";
    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISiteSettingsRepository _settings;
    private readonly ILogger<TurnstileValidationService> _logger;

    public TurnstileValidationService(
        IHttpClientFactory httpClientFactory,
        ISiteSettingsRepository settings,
        ILogger<TurnstileValidationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct).ConfigureAwait(false);
        var secret = settings.CloudflareTurnstileSecretKey;

        if (string.IsNullOrWhiteSpace(secret))
        {
            // Dev mode: no secret configured → accept everything. Production
            // configures the secret + site key via SiteSettings.
            _logger.LogDebug("Turnstile secret not configured; skipping validation.");
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("Turnstile validation rejected: missing token.");
            return false;
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["secret"] = secret,
            ["response"] = token,
        };
        if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp;

        try
        {
            using var response = await client.PostAsync(SiteVerifyUrl, new FormUrlEncodedContent(form), ct)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Turnstile siteverify returned {StatusCode}.", (int)response.StatusCode);
                return false;
            }
            var body = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken: ct)
                .ConfigureAwait(false);
            return body?.Success ?? false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Turnstile siteverify HTTP failure.");
            return false;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Turnstile siteverify timed out.");
            return false;
        }
    }

    private sealed record SiteVerifyResponse(bool Success);
}
