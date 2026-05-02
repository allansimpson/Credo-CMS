namespace CredoCms.Application.Common;

/// <summary>
/// Outbound email abstraction. Phase 1 ships a Serilog-logging implementation only
/// (<c>LoggingEmailService</c>) so that invitation and reset flows are end-to-end
/// testable without an SMTP/SendGrid dependency. Phase 5 introduces the real
/// SendGrid implementation.
/// </summary>
public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>An outbound email payload.</summary>
/// <param name="To">Single primary recipient address.</param>
/// <param name="Subject">Email subject line.</param>
/// <param name="HtmlBody">HTML body. Required.</param>
/// <param name="PlainTextBody">Plain-text fallback. Optional but strongly preferred.</param>
/// <param name="Tags">Free-form tags surfaced in logs / email-provider metadata for filtering.</param>
public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null,
    IReadOnlyDictionary<string, string>? Tags = null);
