using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

/// <summary>
/// Sends a one-shot "Email is configured correctly" message to verify a
/// candidate provider config <em>before</em> the admin saves it. Avoids
/// the chicken-and-egg of "save the broken config to test it."
/// </summary>
public interface ITestEmailService
{
    Task<TestEmailResult> SendAsync(TestEmailConfig config, string toAddress, string toName, CancellationToken ct = default);
}

/// <summary>Candidate provider config — mirrors the email-related subset
/// of <c>UpdateSiteSettingsRequest</c>.</summary>
public sealed record TestEmailConfig(
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
    string? TestEmailRecipient);

public sealed record TestEmailResult(bool Success, string? ErrorMessage, string? Note);
