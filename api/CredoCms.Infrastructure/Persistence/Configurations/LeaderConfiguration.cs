using CredoCms.Domain.Leaders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class LeaderConfiguration : IEntityTypeConfiguration<Leader>
{
    public void Configure(EntityTypeBuilder<Leader> builder)
    {
        builder.ToTable("Leaders");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.Category, x.DisplayOrder });
        builder.Property(x => x.BioJson).HasColumnType("nvarchar(max)");
        // Intentionally not temporal (per VERSIONING.md §2).
    }
}
