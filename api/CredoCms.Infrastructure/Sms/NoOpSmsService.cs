using CredoCms.Application.Sms;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Sms;

/// <summary>
/// v1 default <see cref="ISmsService"/>. Logs at WARN ("SMS not
/// configured: …") and never sends. Replaced in v1.5 by Twilio.
/// </summary>
public sealed class NoOpSmsService : ISmsService
{
    private readonly ILogger<NoOpSmsService> _logger;
    public NoOpSmsService(ILogger<NoOpSmsService> logger) => _logger = logger;

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _logger.LogWarning(
            "[NoOpSmsService] SMS not configured — message to {ToNumber} (userId={UserId}) was not sent: {Body}",
            message.ToNumber, message.UserId, message.Body);
        return Task.CompletedTask;
    }
}
