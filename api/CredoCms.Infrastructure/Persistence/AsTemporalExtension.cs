using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence;

/// <summary>
/// Convention helper for declaring system-versioned temporal tables. The
/// table itself is named by EF inference (already set by <c>ToTable</c> or
/// the entity's type name); the history table mirrors that with a
/// <c>History</c> suffix.
/// </summary>
internal static class AsTemporalExtension
{
    /// <summary>
    /// Declares the entity's primary table as system-versioned. The history
    /// table is named <c>{tableName}History</c> with periods <c>ValidFrom</c>
    /// and <c>ValidTo</c> (matching <c>VERSIONING.md</c> §5).
    /// </summary>
    public static EntityTypeBuilder<TEntity> AsTemporal<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string tableName)
        where TEntity : class
    {
        builder.ToTable(tableName, t => t.IsTemporal(temp =>
        {
            temp.HasPeriodStart("ValidFrom");
            temp.HasPeriodEnd("ValidTo");
            temp.UseHistoryTable($"{tableName}History");
        }));
        return builder;
    }
}
