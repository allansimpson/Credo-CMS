using CredoCms.Application.Common;
using CredoCms.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CredoCms.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that stamps <see cref="IVersionedEntity"/> writes with the
/// acting user (<see cref="ICurrentUserService.UserId"/>) and current UTC timestamp.
/// Registered globally on <see cref="ApplicationDbContext"/>.
/// </summary>
public sealed class VersioningInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public VersioningInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var actingUserId = _currentUser.UserId;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IVersionedEntity versioned)
            {
                continue;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                versioned.ModifiedByUserId = actingUserId;
                versioned.ModifiedAt = now;
            }
        }
    }
}
