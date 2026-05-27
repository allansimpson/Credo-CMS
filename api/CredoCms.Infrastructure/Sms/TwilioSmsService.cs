using CredoCms.Application.Sms;

namespace CredoCms.Infrastructure.Sms;

/// <summary>
/// v1.5 placeholder. The class exists so v1.5 can drop in a real
/// Twilio implementation without changing DI signatures or DTOs. v1
/// callers always resolve <see cref="NoOpSmsService"/> as
/// <see cref="ISmsService"/>; constructing this class throws so an
/// accidental DI change surfaces loudly.
/// </summary>
public sealed class TwilioSmsService : ISmsService
{
    public TwilioSmsService()
    {
        throw new NotImplementedException(
            "SMS via Twilio is not implemented in v1; planned for v1.5.");
    }

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
