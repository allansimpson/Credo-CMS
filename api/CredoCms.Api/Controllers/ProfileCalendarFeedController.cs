using System.Security.Claims;
using CredoCms.Application.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Profile endpoints for the per-member iCal feed. The plaintext token is
/// returned on issue but never stored — only a SHA-256 hash. Re-issuing
/// always invalidates the prior URL.
/// </summary>
[ApiController]
[Route("api/profile/calendar-feed")]
[Authorize]
public sealed class ProfileCalendarFeedController : ControllerBase
{
    private readonly ICalendarFeedTokenService _tokens;
    public ProfileCalendarFeedController(ICalendarFeedTokenService tokens) => _tokens = tokens;

    public sealed record FeedTokenStatusResponse(bool HasToken, DateTimeOffset? CreatedAt, DateTimeOffset? LastUsedAt);
    public sealed record IssueTokenResponse(string Token, string Url);

    [HttpGet]
    public async Task<ActionResult<FeedTokenStatusResponse>> GetAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var info = await _tokens.GetCurrentAsync(userId, ct);
        return Ok(new FeedTokenStatusResponse(info is not null, info?.CreatedAt, info?.LastUsedAt));
    }

    [HttpPost("issue")]
    public async Task<ActionResult<IssueTokenResponse>> IssueAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var token = await _tokens.IssueAsync(userId, ct);
        var url = $"{Request.Scheme}://{Request.Host}/calendar/feed/{token}.ics";
        return Ok(new IssueTokenResponse(token, url));
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> RevokeAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await _tokens.RevokeAllAsync(userId, ct);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }
}
