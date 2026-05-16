using System.Security.Cryptography;
using System.Text;

namespace CredoCms.Application.Events;

/// <summary>
/// Signs a "cancel my registration" token. The token encodes
/// <c>{registrationId}|{expiresAtUtcUnix}|{base64(hmac)}</c> with HMAC-SHA256
/// keyed by a server-side secret. Validates expiry + signature.
///
/// The secret comes from configuration (see RegistrationTokenSignerOptions).
/// </summary>
public sealed class RegistrationTokenSignerOptions
{
    public const string SectionName = "EventRegistration";

    /// <summary>The default value compared against at startup; deployments
    /// running in Production with this exact value get a hard fail.</summary>
    public const string DefaultDevSecret =
        "credo-cms-dev-token-secret-change-me-in-production-please-do-it";

    /// <summary>Base64 or raw string secret. Must be >= 32 chars in
    /// production AND must not equal <see cref="DefaultDevSecret"/>.</summary>
    public string TokenSigningSecret { get; set; } = DefaultDevSecret;
}

public interface IRegistrationTokenSigner
{
    string Sign(Guid registrationId, TimeSpan validity);
    bool TryValidate(string token, out Guid registrationId);
}

public sealed class RegistrationTokenSigner : IRegistrationTokenSigner
{
    private readonly byte[] _key;

    public RegistrationTokenSigner(RegistrationTokenSignerOptions options)
    {
        _key = Encoding.UTF8.GetBytes(options.TokenSigningSecret ?? string.Empty);
        if (_key.Length < 16)
            throw new InvalidOperationException("RegistrationTokenSigner secret must be at least 16 chars.");
    }

    public string Sign(Guid registrationId, TimeSpan validity)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(validity).ToUnixTimeSeconds();
        var body = $"{registrationId:N}|{expiresAt}";
        var sig = ComputeSignature(body);
        var raw = $"{body}|{Convert.ToBase64String(sig)}";
        return Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
    }

    public bool TryValidate(string token, out Guid registrationId)
    {
        registrationId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var raw = Encoding.UTF8.GetString(Base64UrlDecode(token));
            var parts = raw.Split('|');
            if (parts.Length != 3) return false;

            if (!Guid.TryParseExact(parts[0], "N", out var id)) return false;
            if (!long.TryParse(parts[1], out var exp)) return false;
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return false;

            var expected = ComputeSignature($"{parts[0]}|{parts[1]}");
            var actual = Convert.FromBase64String(parts[2]);
            if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return false;

            registrationId = id;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private byte[] ComputeSignature(string body)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}
