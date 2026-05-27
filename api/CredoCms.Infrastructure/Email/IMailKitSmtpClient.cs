using MailKit.Security;
using MimeKit;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Thin abstraction over <see cref="MailKit.Net.Smtp.SmtpClient"/> covering
/// only the methods the SMTP send loop uses. Exists so tests can
/// mock the transport without spinning up a real SMTP server. The default
/// impl is a one-line forwarder.
/// </summary>
public interface IMailKitSmtpClient : IDisposable
{
    Task ConnectAsync(string host, int port, SecureSocketOptions options, CancellationToken ct);
    Task AuthenticateAsync(string user, string pass, CancellationToken ct);
    Task<string> SendAsync(MimeMessage message, CancellationToken ct);
    Task DisconnectAsync(bool quit, CancellationToken ct);
}

/// <summary>Factory for <see cref="IMailKitSmtpClient"/>. A fresh client
/// per send keeps the connection short-lived and avoids tying a request
/// to a long-running TCP connection (matches the project's stateless
/// scope-per-request model).</summary>
public interface IMailKitSmtpClientFactory
{
    IMailKitSmtpClient Create();
}

internal sealed class MailKitSmtpClient : IMailKitSmtpClient
{
    private readonly MailKit.Net.Smtp.SmtpClient _inner = new();

    public Task ConnectAsync(string host, int port, SecureSocketOptions options, CancellationToken ct) =>
        _inner.ConnectAsync(host, port, options, ct);

    public Task AuthenticateAsync(string user, string pass, CancellationToken ct) =>
        _inner.AuthenticateAsync(user, pass, ct);

    public Task<string> SendAsync(MimeMessage message, CancellationToken ct) =>
        _inner.SendAsync(message, ct);

    public Task DisconnectAsync(bool quit, CancellationToken ct) =>
        _inner.DisconnectAsync(quit, ct);

    public void Dispose() => _inner.Dispose();
}

internal sealed class MailKitSmtpClientFactory : IMailKitSmtpClientFactory
{
    public IMailKitSmtpClient Create() => new MailKitSmtpClient();
}
