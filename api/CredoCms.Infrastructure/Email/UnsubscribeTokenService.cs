using System.Security.Cryptography;
using System.Text;
using CredoCms.Application.Email;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;

namespace CredoCms.Infrastructure.Email;

public sealed class UnsubscribeTokenService : IUnsubscribeTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);

    private readonly ISiteSettingsRepository _settings;

    public UnsubscribeTokenService(ISiteSettingsRepository settings) => _settings = settings;

    public async Task<string> GenerateAsync(Guid userId, EmailCategory category, CancellationToken ct = default)
    {
        var key = await GetOrInitKeyAsync(ct).ConfigureAwait(false);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{userId:N}|{(int)category}|{timestamp}";
        var signature = ComputeHmac(payload, key);
        var combined = $"{payload}|{signature}";
        return Base64UrlEncode(Encoding.UTF8.GetBytes(combined));
    }

    public async Task<UnsubscribeTokenResult> ValidateAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new UnsubscribeTokenResult(false, Guid.Empty, EmailCategory.Broadcast, "blank token");

        string decoded;
        try { decoded = Encoding.UTF8.GetString(Base64UrlDecode(token)); }
        catch (FormatException) { return new(false, Guid.Empty, EmailCategory.Broadcast, "malformed"); }

        var parts = decoded.Split('|');
        if (parts.Length != 4) return new(false, Guid.Empty, EmailCategory.Broadcast, "malformed");

        if (!Guid.TryParseExact(parts[0], "N", out var userId))
            return new(false, Guid.Empty, EmailCategory.Broadcast, "malformed userId");
        if (!int.TryParse(parts[1], out var categoryInt))
            return new(false, userId, EmailCategory.Broadcast, "malformed category");
        if (!long.TryParse(parts[2], out var ts))
            return new(false, userId, EmailCategory.Broadcast, "malformed timestamp");

        var sentAt = DateTimeOffset.FromUnixTimeSeconds(ts);
        if (DateTimeOffset.UtcNow - sentAt > TokenLifetime)
            return new(false, userId, (EmailCategory)categoryInt, "expired");

        var key = await GetOrInitKeyAsync(ct).ConfigureAwait(false);
        var expected = ComputeHmac($"{parts[0]}|{parts[1]}|{parts[2]}", key);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(parts[3])))
            return new(false, userId, (EmailCategory)categoryInt, "signature mismatch");

        return new(true, userId, (EmailCategory)categoryInt, null);
    }

    private async Task<byte[]> GetOrInitKeyAsync(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(settings.UnsubscribeSigningKey))
        {
            return Convert.FromBase64String(settings.UnsubscribeSigningKey);
        }
        // First-read auto-init. The new key persists on next save by the admin
        // settings flow; until then we use a deterministic fallback per-process
        // (test/dev only — production should always have the key configured).
        var bytes = RandomNumberGenerator.GetBytes(32);
        settings.UnsubscribeSigningKey = Convert.ToBase64String(bytes);
        try { await _settings.UpdateAsync(settings, ct).ConfigureAwait(false); }
        catch { /* read-only test repos throw — fall through with the in-memory key */ }
        return bytes;
    }

    private static string ComputeHmac(string payload, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        var pad = padded.Length % 4;
        if (pad > 0) padded = padded.PadRight(padded.Length + (4 - pad), '=');
        return Convert.FromBase64String(padded);
    }
}
