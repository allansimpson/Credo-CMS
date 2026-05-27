using System.Security.Claims;
using CredoCms.Application.Email;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/site-settings")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public sealed class SiteSettingsAdminController : ControllerBase
{
    private readonly ISiteSettingsService _service;
    private readonly ITestEmailService _testEmail;

    public SiteSettingsAdminController(ISiteSettingsService service, ITestEmailService testEmail)
    {
        _service = service;
        _testEmail = testEmail;
    }

    [HttpGet]
    public Task<SiteSettingsDto> GetAsync(CancellationToken ct) => _service.GetAsync(ct);

    [HttpPut]
    public async Task<ActionResult<SiteSettingsDto>> UpdateAsync(
        [FromBody] UpdateSiteSettingsRequest request, CancellationToken ct)
    {
        try
        {
            var updated = await _service.UpdateAsync(request, ct);
            return Ok(updated);
        }
        catch (OptimisticConcurrencyException ex)
        {
            return Conflict(new { errors = new[] { ex.Message } });
        }
    }

    /// <summary>Sends a test email using the supplied (in-flight, possibly
    /// unsaved) provider config to the current admin's address — or to
    /// <c>OverrideToAddress</c> when the admin wants to verify against a
    /// specific mailbox. Always returns <c>200 OK</c>; the body's
    /// <c>success</c> flag is the actual outcome.</summary>
    [HttpPost("test-email")]
    public async Task<ActionResult<TestEmailResult>> TestEmailAsync(
        [FromBody] TestEmailRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var toAddress = !string.IsNullOrWhiteSpace(request.OverrideToAddress)
            ? request.OverrideToAddress!
            : User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            return Ok(new TestEmailResult(
                Success: false,
                ErrorMessage: "No recipient — current user has no email claim and no OverrideToAddress was supplied.",
                Note: null));
        }

        var toName = User.FindFirstValue(ClaimTypes.GivenName) ?? "Administrator";

        var config = new TestEmailConfig(
            Provider: request.Provider,
            EmailFromAddress: request.EmailFromAddress,
            EmailFromName: request.EmailFromName,
            EmailReplyToAddress: request.EmailReplyToAddress,
            SendGridApiKey: request.SendGridApiKey,
            SmtpHost: request.SmtpHost,
            SmtpPort: request.SmtpPort,
            SmtpUsername: request.SmtpUsername,
            SmtpPassword: request.SmtpPassword,
            SmtpUseSsl: request.SmtpUseSsl,
            TestEmailRecipient: request.TestEmailRecipient);

        var result = await _testEmail.SendAsync(config, toAddress, toName, ct);
        return Ok(result);
    }
}

/// <summary>Test-send request payload mirroring the email-related subset
/// of <see cref="UpdateSiteSettingsRequest"/> plus an optional explicit
/// recipient.</summary>
public sealed record TestEmailRequest(
    EmailProvider Provider,
    string EmailFromAddress,
    string EmailFromName,
    string? EmailReplyToAddress,
    string? SendGridApiKey,
    string? SmtpHost,
    int SmtpPort,
    string? SmtpUsername,
    string? SmtpPassword,
    bool SmtpUseSsl,
    string? TestEmailRecipient,
    string? OverrideToAddress);
