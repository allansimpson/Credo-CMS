namespace CredoCms.Application.Sms;

/// <summary>
/// Outbound SMS abstraction. v1 ships
/// <see cref="NoOpSmsService"/> only; <see cref="TwilioSmsService"/> exists
/// as a structural placeholder so v1.5 can drop in the implementation
/// without schema or interface changes.
/// </summary>
public interface ISmsService
{
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
}

public sealed record SmsMessage(string ToNumber, string Body, Guid? UserId = null);
