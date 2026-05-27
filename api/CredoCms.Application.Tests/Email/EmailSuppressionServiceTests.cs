using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using Moq;

namespace CredoCms.Application.Tests.Email;

public sealed class EmailSuppressionServiceTests
{
    private static (EmailSuppressionService Sut, Mock<IEmailSuppressionRepository> Repo, Mock<IAuditLogger> Audit)
        MakeSut()
    {
        var repo = new Mock<IEmailSuppressionRepository>();
        var audit = new Mock<IAuditLogger>();
        return (new EmailSuppressionService(repo.Object, audit.Object), repo, audit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsSuppressed_returns_false_for_blank(string? input)
    {
        var (sut, _, _) = MakeSut();
        (await sut.IsSuppressedAsync(input!)).Should().BeFalse();
    }

    [Fact]
    public async Task IsSuppressed_normalizes_to_lowercase_before_lookup()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.GetByEmailAsync("user@example.org", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSuppression { EmailAddress = "user@example.org" });

        (await sut.IsSuppressedAsync("  USER@Example.org  ")).Should().BeTrue();

        repo.Verify(r => r.GetByEmailAsync("user@example.org", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkLookup_returns_only_addresses_present_in_suppression_list()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.BulkLookupAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, EmailSuppression>
            {
                ["bouncer@example.org"] = new() { EmailAddress = "bouncer@example.org" },
            });

        var result = await sut.BulkLookupAsync(new[] { "Bouncer@Example.org", "ok@example.org" });

        result.Should().BeEquivalentTo(new[] { "bouncer@example.org" });
    }

    [Fact]
    public async Task BulkLookup_returns_empty_for_empty_input()
    {
        var (sut, repo, _) = MakeSut();
        var result = await sut.BulkLookupAsync(Array.Empty<string>());
        result.Should().BeEmpty();
        repo.Verify(r => r.BulkLookupAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkLookup_filters_blank_entries_before_querying()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.BulkLookupAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, EmailSuppression>());

        await sut.BulkLookupAsync(new[] { "", "   ", "real@example.org" });

        repo.Verify(r => r.BulkLookupAsync(
            It.Is<IReadOnlyCollection<string>>(c => c.Count == 1 && c.Contains("real@example.org")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_normalizes_audit_logs_and_upserts()
    {
        var (sut, repo, audit) = MakeSut();

        await sut.AddAsync("BAD@Example.org", SuppressionType.HardBounce, SuppressionSource.SendGridWebhook, "550 5.1.1");

        repo.Verify(r => r.UpsertAsync(
            It.Is<EmailSuppression>(s =>
                s.EmailAddress == "bad@example.org" &&
                s.SuppressionType == SuppressionType.HardBounce &&
                s.CreatedSource == SuppressionSource.SendGridWebhook &&
                s.Reason == "550 5.1.1"),
            It.IsAny<CancellationToken>()), Times.Once);

        audit.Verify(a => a.WriteAsync(
            "EmailSuppression.Added",
            nameof(EmailSuppression),
            "bad@example.org",
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_normalizes_audit_logs_and_deletes()
    {
        var (sut, repo, audit) = MakeSut();

        await sut.RemoveAsync("Old@EXAMPLE.org");

        repo.Verify(r => r.RemoveAsync("old@example.org", It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(a => a.WriteAsync(
            "EmailSuppression.Removed",
            nameof(EmailSuppression),
            "old@example.org",
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_throws_when_email_blank()
    {
        var (sut, _, _) = MakeSut();
        await FluentActions.Invoking(() => sut.AddAsync("", SuppressionType.ManualSuppression, SuppressionSource.Admin, null))
            .Should().ThrowAsync<ArgumentException>();
    }
}
