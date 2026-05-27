using CredoCms.Domain.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class SearchIndexEntryConfiguration : IEntityTypeConfiguration<SearchIndexEntry>
{
    public void Configure(EntityTypeBuilder<SearchIndexEntry> builder)
    {
        builder.ToTable("SearchIndex");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique();
        builder.HasIndex(x => x.IsPublished);
        builder.HasIndex(x => x.IsMembersOnly);
        builder.Property(x => x.BodyText).HasColumnType("nvarchar(max)");
        // Intentionally not temporal — the index is a derived projection.
    }
}
