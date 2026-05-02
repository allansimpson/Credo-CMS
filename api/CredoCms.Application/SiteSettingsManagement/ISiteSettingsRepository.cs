using CredoCms.Domain.Settings;

namespace CredoCms.Application.SiteSettingsManagement;

/// <summary>
/// Read/write access for the single Site Settings row.
/// </summary>
public interface ISiteSettingsRepository
{
    /// <summary>Returns the current Site Settings row. Throws if it has not been seeded.</summary>
    Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the Site Settings row. Throws <see cref="OptimisticConcurrencyException"/>
    /// when the supplied <see cref="SiteSettings.RowVersion"/> doesn't match the
    /// stored token.
    /// </summary>
    Task UpdateAsync(SiteSettings settings, CancellationToken cancellationToken = default);
}

public sealed class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException()
        : base("The record was modified by another user. Reload and try again.")
    {
    }

    public OptimisticConcurrencyException(string message) : base(message) { }
    public OptimisticConcurrencyException(string message, Exception inner) : base(message, inner) { }
}
