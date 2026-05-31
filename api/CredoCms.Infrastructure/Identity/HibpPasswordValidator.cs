using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CredoCms.Infrastructure.Identity;

/// <summary>
/// Plugs into ASP.NET Core Identity's password-validation pipeline so every
/// password set or changed is screened against the Have-I-Been-Pwned breach
/// corpus. Uses the k-anonymity API: only the first 5 characters of the
/// SHA-1 hash of the candidate password leave the server. HIBP returns every
/// suffix whose hash begins with that prefix; we look for our specific
/// suffix locally. The full hash — and therefore the password — is never
/// transmitted.
///
/// SHA-1 here is fine even though it's deprecated for signatures: we're not
/// authenticating with it, just doing a hash-lookup against a public
/// breach-corpus index that's keyed on SHA-1 by design.
/// </summary>
public sealed class HibpPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public const string HttpClientName = "PwnedPasswords";

    private readonly IHttpClientFactory _factory;
    private readonly HibpPasswordValidatorOptions _options;
    private readonly ILogger<HibpPasswordValidator> _logger;

    public HibpPasswordValidator(
        IHttpClientFactory factory,
        IOptions<HibpPasswordValidatorOptions> options,
        ILogger<HibpPasswordValidator> logger)
    {
        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (!_options.Enabled || string.IsNullOrEmpty(password))
        {
            return IdentityResult.Success;
        }

        var sha1Hex = ComputeSha1Hex(password);
        var prefix = sha1Hex[..5];
        var suffix = sha1Hex[5..];

        try
        {
            using var http = _factory.CreateClient(HttpClientName);
            using var resp = await http.GetAsync($"range/{prefix}").ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "HIBP range API returned {Status} for prefix {Prefix}; AllowOnApiFailure={Allow}.",
                    resp.StatusCode, prefix, _options.AllowOnApiFailure);
                return _options.AllowOnApiFailure ? IdentityResult.Success : ApiUnavailableFailure();
            }

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Response is text/plain, one record per line: SHA1_SUFFIX:COUNT
            // SHA1_SUFFIX is 35 uppercase hex chars (40 - 5 prefix).
            foreach (var rawLine in body.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;

                var colonIdx = line.IndexOf(':');
                if (colonIdx <= 0) continue;

                var lineSuffix = line.AsSpan(0, colonIdx);
                if (lineSuffix.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var countSpan = line.AsSpan(colonIdx + 1).Trim();
                    var breachCount = int.TryParse(countSpan, out var c) ? c : 1;
                    if (breachCount > _options.MaxBreachCount)
                    {
                        return CompromisedFailure(breachCount);
                    }
                    break;
                }
            }

            return IdentityResult.Success;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex,
                "HIBP range API call failed; AllowOnApiFailure={Allow}.",
                _options.AllowOnApiFailure);
            return _options.AllowOnApiFailure ? IdentityResult.Success : ApiUnavailableFailure();
        }
    }

    // SHA-1 here is dictated by the HIBP protocol — its k-anonymity index is keyed on
    // SHA-1 by design. We're not using it for authentication, signing, or storing
    // anything; it's only a lookup key against a public breach corpus. CA5350's
    // "weak algorithm" concern doesn't apply to this use.
#pragma warning disable CA5350
    private static string ComputeSha1Hex(string password)
    {
        Span<byte> hash = stackalloc byte[20]; // SHA-1 = 160 bits
        var written = SHA1.HashData(Encoding.UTF8.GetBytes(password), hash);
        return Convert.ToHexString(hash[..written]); // uppercase, matches HIBP's response format
    }
#pragma warning restore CA5350

    private static IdentityResult CompromisedFailure(int breachCount) =>
        IdentityResult.Failed(new IdentityError
        {
            Code = "PasswordCompromised",
            Description =
                $"This password has appeared in {breachCount:N0} known data breaches " +
                "and is considered compromised. Please choose a different one — " +
                "a unique passphrase that you don't reuse anywhere else is strongest.",
        });

    private static IdentityResult ApiUnavailableFailure() =>
        IdentityResult.Failed(new IdentityError
        {
            Code = "PasswordBreachCheckFailed",
            Description =
                "We couldn't verify this password against the breach database right now. " +
                "Please try again in a moment.",
        });
}
