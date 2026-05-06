using SendGrid.Helpers.EventWebhook;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Wraps SendGrid's <see cref="RequestValidator"/> ECDSA verification so
/// the webhook controller can be tested without needing a real public key.
/// SendGrid signs each event-webhook POST with the ECDSA key configured
/// in their dashboard; the verifier checks <c>signature</c> against
/// <c>timestamp + body</c>.
/// </summary>
public interface ISendGridWebhookVerifier
{
    /// <summary>True when the supplied signature/timestamp validate against
    /// the configured public key. Returns false when the public key is
    /// blank — caller decides whether to allow that (test/dev) or reject
    /// (prod).</summary>
    bool Verify(string publicKey, string body, string signature, string timestamp);
}

internal sealed class SendGridWebhookVerifier : ISendGridWebhookVerifier
{
    public bool Verify(string publicKey, string body, string signature, string timestamp)
    {
        if (string.IsNullOrWhiteSpace(publicKey)) return false;
        try
        {
            var validator = new RequestValidator();
            var ecdsaKey = validator.ConvertPublicKeyToECDSA(publicKey);
            return validator.VerifySignature(ecdsaKey, body, signature, timestamp);
        }
        catch
        {
            // Malformed key, malformed signature, etc. → reject.
            return false;
        }
    }
}
