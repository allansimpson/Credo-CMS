using CredoCms.Application.Events;

namespace CredoCms.Application.Tests.Events;

public sealed class RegistrationTokenSignerTests
{
    private static RegistrationTokenSigner MakeSigner() =>
        new(new RegistrationTokenSignerOptions { TokenSigningSecret = "test-secret-32-chars-long-enough!" });

    [Fact]
    public void RoundTrip_validates_a_freshly_signed_token()
    {
        var signer = MakeSigner();
        var id = Guid.NewGuid();
        var token = signer.Sign(id, TimeSpan.FromMinutes(10));
        signer.TryValidate(token, out var got).Should().BeTrue();
        got.Should().Be(id);
    }

    [Fact]
    public void Tampered_token_fails_validation()
    {
        var signer = MakeSigner();
        var token = signer.Sign(Guid.NewGuid(), TimeSpan.FromMinutes(10));
        // Replace the last character to corrupt the signature.
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');
        signer.TryValidate(tampered, out _).Should().BeFalse();
    }

    [Fact]
    public void Expired_token_fails_validation()
    {
        var signer = MakeSigner();
        var token = signer.Sign(Guid.NewGuid(), TimeSpan.FromSeconds(-30));
        signer.TryValidate(token, out _).Should().BeFalse();
    }

    [Fact]
    public void Token_signed_with_different_secret_fails()
    {
        var s1 = new RegistrationTokenSigner(new RegistrationTokenSignerOptions
        { TokenSigningSecret = "secret-one-thirty-two-chars-yep!" });
        var s2 = new RegistrationTokenSigner(new RegistrationTokenSignerOptions
        { TokenSigningSecret = "secret-two-thirty-two-chars-yep!" });
        var token = s1.Sign(Guid.NewGuid(), TimeSpan.FromMinutes(10));
        s2.TryValidate(token, out _).Should().BeFalse();
    }
}
