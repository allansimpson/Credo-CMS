namespace CredoCms.Application.Common;

/// <summary>
/// Marker / unit-of-work for the Application layer. Concrete repositories
/// (ISiteSettingsRepository, IAuditLogRepository, ...) handle their own writes.
/// This interface exists to give services a single place to commit cross-repository
/// changes when a flow needs that — current flows don't, but the seam is here for
/// future use. The EF Core implementation lives in Infrastructure.
/// </summary>
/// <remarks>
/// Per the architectural rule, the Application project does not reference
/// Microsoft.EntityFrameworkCore directly — repositories return materialized
/// data (Task&lt;T&gt;, Task&lt;List&lt;T&gt;&gt;, etc.) rather than IQueryable.
/// </remarks>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
