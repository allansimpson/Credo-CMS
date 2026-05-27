using System.Security.Cryptography;
using CredoCms.Application.Calendar;
using CredoCms.Domain.Events;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Calendar;

public sealed class CalendarFeedTokenService : ICalendarFeedTokenService
{
    private readonly ApplicationDbContext _db;
    public CalendarFeedTokenService(ApplicationDbContext db) => _db = db;

    public async Task<string> IssueAsync(Guid userId, CancellationToken ct = default)
    {
        // Revoke any existing active token first — one feed URL per user.
        await _db.CalendarFeedTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow), ct)
            .ConfigureAwait(false);

        var token = GenerateRandomToken();
        var entity = new CalendarFeedToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(token),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.CalendarFeedTokens.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return token;
    }

    public async Task<Guid?> ResolveAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var hash = HashToken(token);
        var entity = await _db.CalendarFeedTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct)
            .ConfigureAwait(false);
        if (entity is null) return null;

        entity.LastUsedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity.UserId;
    }

    public async Task<CalendarFeedTokenInfo?> GetCurrentAsync(Guid userId, CancellationToken ct = default)
    {
        var t = await _db.CalendarFeedTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return t is null ? null : new CalendarFeedTokenInfo(t.UserId, t.CreatedAt, t.LastUsedAt);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken ct = default)
    {
        await _db.CalendarFeedTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow), ct)
            .ConfigureAwait(false);
    }

    private static string GenerateRandomToken()
    {
        // 32 random bytes → URL-safe base64 (~43 chars).
        Span<byte> buf = stackalloc byte[32];
        RandomNumberGenerator.Fill(buf);
        return Convert.ToBase64String(buf)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
