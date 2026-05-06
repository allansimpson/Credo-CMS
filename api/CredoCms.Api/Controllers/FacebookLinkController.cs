using System.Security.Claims;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Facebook account linking. Q15 ships the linking flow only — we do NOT
/// allow Facebook OAuth to create new accounts. Members start by signing in
/// with email + password, then link Facebook from their profile. After
/// linking, subsequent sign-ins via "Continue with Facebook" succeed because
/// the AspNetUserLogins row exists.
/// </summary>
[ApiController]
[Route("api/auth/facebook")]
public sealed class FacebookLinkController : ControllerBase
{
    private const string ProviderKey = FacebookDefaults.AuthenticationScheme;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public FacebookLinkController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    /// <summary>
    /// Anonymous-friendly entry: starts an OAuth challenge, returning to
    /// /api/auth/facebook/sign-in-callback. Used by the "Continue with
    /// Facebook" button on /login.
    /// </summary>
    [HttpGet("sign-in-challenge")]
    [AllowAnonymous]
    public IActionResult SignInChallenge([FromQuery] string? returnUrl = "/")
    {
        var redirect = Url.Action(nameof(SignInCallbackAsync), "FacebookLink",
            new { returnUrl }) ?? "/";
        var props = _signInManager.ConfigureExternalAuthenticationProperties(
            ProviderKey, redirect);
        return Challenge(props, ProviderKey);
    }

    /// <summary>
    /// Sign-in callback. We refuse to create an account from a fresh
    /// Facebook profile — only members who've already linked from /profile
    /// can authenticate via Facebook. Unknown linkings get redirected to
    /// /login with an error code.
    /// </summary>
    [HttpGet("sign-in-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> SignInCallbackAsync([FromQuery] string? returnUrl = "/")
    {
        var info = await _signInManager.GetExternalLoginInfoAsync().ConfigureAwait(false);
        if (info is null)
        {
            return Redirect("/login?error=facebook_no_info");
        }

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey,
            isPersistent: true, bypassTwoFactor: true).ConfigureAwait(false);

        if (result.Succeeded)
        {
            return Redirect(returnUrl ?? "/");
        }

        // Hard rule: do not create accounts. The user must sign in with
        // password first and link Facebook from their profile. The login
        // page surfaces this error code with a friendly message.
        return Redirect("/login?error=facebook_not_linked");
    }

    /// <summary>
    /// Authenticated link flow: starts an OAuth challenge whose callback
    /// will associate the returned login with the current user.
    /// </summary>
    [HttpGet("link-challenge")]
    [Authorize]
    public IActionResult LinkChallenge([FromQuery] string? returnUrl = "/profile")
    {
        var redirect = Url.Action(nameof(LinkCallbackAsync), "FacebookLink",
            new { returnUrl }) ?? "/profile";
        var props = _signInManager.ConfigureExternalAuthenticationProperties(
            ProviderKey, redirect, _userManager.GetUserId(User));
        return Challenge(props, ProviderKey);
    }

    /// <summary>
    /// Authenticated link callback. Stores the Facebook login on the
    /// current user's AspNetUserLogins row.
    /// </summary>
    [HttpGet("link-callback")]
    [Authorize]
    public async Task<IActionResult> LinkCallbackAsync([FromQuery] string? returnUrl = "/profile")
    {
        var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
        if (user is null) return Unauthorized();

        var info = await _signInManager.GetExternalLoginInfoAsync(user.Id.ToString()).ConfigureAwait(false);
        if (info is null) return Redirect((returnUrl ?? "/profile") + "?error=facebook_no_info");

        var result = await _userManager.AddLoginAsync(user, info).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return Redirect((returnUrl ?? "/profile") + "?error=facebook_link_failed");
        }
        await _signInManager.UpdateExternalAuthenticationTokensAsync(info).ConfigureAwait(false);
        return Redirect((returnUrl ?? "/profile") + "?facebook=linked");
    }

    /// <summary>Authenticated unlink — removes the Facebook AspNetUserLogins row.</summary>
    [HttpPost("unlink")]
    [Authorize]
    public async Task<IActionResult> UnlinkAsync(CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
        if (user is null) return Unauthorized();
        var logins = await _userManager.GetLoginsAsync(user).ConfigureAwait(false);
        var fb = logins.FirstOrDefault(l => l.LoginProvider == ProviderKey);
        if (fb is null) return NoContent();
        var result = await _userManager.RemoveLoginAsync(user, fb.LoginProvider, fb.ProviderKey).ConfigureAwait(false);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }

    /// <summary>Returns whether the current user has a Facebook login linked.</summary>
    [HttpGet("/api/profile/facebook-status")]
    [Authorize]
    public async Task<ActionResult<object>> FacebookStatusAsync()
    {
        var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
        if (user is null) return Unauthorized();
        var logins = await _userManager.GetLoginsAsync(user).ConfigureAwait(false);
        var fb = logins.FirstOrDefault(l => l.LoginProvider == ProviderKey);
        return Ok(new
        {
            isLinked = fb is not null,
            providerKey = fb?.ProviderKey,
        });
    }
}
