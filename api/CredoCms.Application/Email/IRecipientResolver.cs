using CredoCms.Application.Common;
using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

/// <summary>
/// Resolves a target audience selector to a concrete recipient list.
/// Applies (in order): membership filter → category-specific preference
/// filter → suppression-list bulk lookup → dedupe. Recipients carry the
/// per-user merge fields (firstName, lastName) the renderer needs.
///
/// <para>Resolution happens at <em>send</em> time, not compose time, so
/// members joining/leaving groups between draft and dispatch are picked
/// up automatically.</para>
/// </summary>
public interface IRecipientResolver
{
    Task<IReadOnlyList<EmailRecipient>> ResolveAsync(
        BroadcastTargetMode targetMode,
        IReadOnlyCollection<Guid>? targetGroupIds,
        EmailCategory category,
        CancellationToken ct = default);

    /// <summary>Returns just the count + a small sample for the compose
    /// UI's preview panel ("This will go to 47 members; sample: …").</summary>
    Task<RecipientPreview> PreviewAsync(
        BroadcastTargetMode targetMode,
        IReadOnlyCollection<Guid>? targetGroupIds,
        EmailCategory category,
        int sampleSize = 8,
        CancellationToken ct = default);
}

public sealed record RecipientPreview(int TotalCount, IReadOnlyList<RecipientPreviewItem> Sample);

public sealed record RecipientPreviewItem(string DisplayName, string EmailAddress);
