using CredoCms.Domain.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
        // Tags are not versioned (per project convention, join-table-style data).
    }
}
