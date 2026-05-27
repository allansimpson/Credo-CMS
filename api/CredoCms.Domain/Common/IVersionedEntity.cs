namespace CredoCms.Domain.Common;

/// <summary>
/// Marker interface for entities whose changes are recorded as system-versioned
/// temporal-table history. The <see cref="Infrastructure"/> layer's
/// <c>VersioningInterceptor</c> populates these properties on save; any write that
/// bypasses the interceptor will fail at the database due to NOT NULL constraints.
/// </summary>
public interface IVersionedEntity
{
    Guid? ModifiedByUserId { get; set; }

    DateTimeOffset ModifiedAt { get; set; }
}
