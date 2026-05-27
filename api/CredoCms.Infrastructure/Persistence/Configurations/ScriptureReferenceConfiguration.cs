using CredoCms.Domain.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class ScriptureReferenceConfiguration : IEntityTypeConfiguration<ScriptureReference>
{
    public void Configure(EntityTypeBuilder<ScriptureReference> builder)
    {
        builder.ToTable("ScriptureReferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Book).HasConversion<int>();

        // Lookup by parent.
        builder.HasIndex(x => new { x.ParentEntityType, x.ParentEntityId });

        // "Browse by Book" queries.
        builder.HasIndex(x => new { x.Book, x.ChapterStart });

        // Not versioned (join-table-style).
    }
}
