using CredoCms.Application.Common;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Phase-1 email service that writes the message to logs instead of sending it.
/// Lets invitation/reset flows be exercised end-to-end without an SMTP/SendGrid
/// dependency. Replaced by a SendGrid implementation in Phase 5.
/// </summary>
public sealed class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger) => _logger = logger;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation(
            "[LoggingEmailService] Email to {To} subject {Subject}\n--- HTML ---\n{Html}\n--- TEXT ---\n{Text}\nTags: {Tags}",
            message.To,
            message.Subject,
            message.HtmlBody,
            message.PlainTextBody ?? "(none)",
            message.Tags is null ? "(none)" : string.Join(",", message.Tags.Select(kv => $"{kv.Key}={kv.Value}")));

        return Task.CompletedTask;
    }
}
