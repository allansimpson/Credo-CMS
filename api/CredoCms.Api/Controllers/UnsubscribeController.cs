using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Phase 5 R12. Anonymous unsubscribe endpoint backing the
/// List-Unsubscribe header (RFC 2369) and List-Unsubscribe-Post header
/// (RFC 8058 one-click). GET renders a confirmation hand-off; POST
/// performs the unsubscribe immediately. Both pivot on a signed token
/// that carries the userId + category.
/// </summary>
[ApiController]
[Route("api/unsubscribe")]
[AllowAnonymous]
public sealed class UnsubscribeController : ControllerBase
{
    private readonly IUnsubscribeTokenService _tokens;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailSuppressionService _suppression;

    public UnsubscribeController(
        IUnsubscribeTokenService tokens,
        UserManager<ApplicationUser> users,
        IEmailSuppressionService suppression)
    {
        _tokens = tokens;
        _users = users;
        _suppression = suppression;
    }

    [HttpGet]
    public async Task<ActionResult<UnsubscribeStatus>> GetAsync(
        [FromQuery] string token, [FromQuery] string? category = null, CancellationToken ct = default)
    {
        var validated = await _tokens.ValidateAsync(token, ct);
        if (!validated.IsValid)
            return BadRequest(new UnsubscribeStatus(false, validated.FailureReason ?? "invalid token", null));

        var user = await _users.FindByIdAsync(validated.UserId.ToString());
        if (user is null) return NotFound(new UnsubscribeStatus(false, "user not found", null));

        var resolvedCategory = ResolveCategory(category, validated.Category);
        return Ok(new UnsubscribeStatus(
            Success: true,
            Message: $"Click the button below to unsubscribe {user.DisplayName} from {resolvedCategory} emails.",
            Email: user.Email));
    }

    [HttpPost]
    public async Task<ActionResult<UnsubscribeStatus>> PostAsync(
        [FromQuery] string token, [FromQuery] string? category = null, CancellationToken ct = default)
    {
        var validated = await _tokens.ValidateAsync(token, ct);
        if (!validated.IsValid)
            return BadRequest(new UnsubscribeStatus(false, validated.FailureReason ?? "invalid token", null));

        var user = await _users.FindByIdAsync(validated.UserId.ToString());
        if (user is null) return NotFound(new UnsubscribeStatus(false, "user not found", null));

        var resolvedCategory = ResolveCategory(category, validated.Category);
        ApplyOptOut(user, resolvedCategory);

        // "all" path also writes to the suppression list — explicit user
        // intent to never receive any email at this address.
        if (string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
        {
            await _suppression.AddAsync(user.Email!, SuppressionType.Unsubscribe,
                SuppressionSource.MemberAction, "User-driven unsubscribe-all", ct);
        }

        await _users.UpdateAsync(user);
        return Ok(new UnsubscribeStatus(true, $"You've been unsubscribed from {resolvedCategory} emails.", user.Email));
    }

    private static EmailCategory ResolveCategory(string? rawFromQuery, EmailCategory tokenCategory)
    {
        if (string.IsNullOrWhiteSpace(rawFromQuery)) return tokenCategory;
        return rawFromQuery.ToLowerInvariant() switch
        {
            "news" => EmailCategory.News,
            "blog" => EmailCategory.Blog,
            "broadcast" => EmailCategory.Broadcast,
            "group" => EmailCategory.GroupCommunication,
            // "all" is the bulk path: token category is preserved but
            // ApplyOptOut treats "all" specially.
            _ => tokenCategory,
        };
    }

    private static void ApplyOptOut(ApplicationUser user, EmailCategory category)
    {
        switch (category)
        {
            case EmailCategory.News: user.ReceiveNewsEmails = false; break;
            case EmailCategory.Blog: user.ReceiveBlogEmails = false; break;
            case EmailCategory.Broadcast: user.ReceiveBroadcastEmails = false; break;
            case EmailCategory.GroupCommunication: user.ReceiveGroupEmailsGlobal = false; break;
            default:
                // Unknown / Transactional: treat as "all" — clear every
                // non-transactional preference.
                user.ReceiveNewsEmails = false;
                user.ReceiveBlogEmails = false;
                user.ReceiveBroadcastEmails = false;
                user.ReceiveGroupEmailsGlobal = false;
                break;
        }
    }
}

public sealed record UnsubscribeStatus(bool Success, string Message, string? Email);
