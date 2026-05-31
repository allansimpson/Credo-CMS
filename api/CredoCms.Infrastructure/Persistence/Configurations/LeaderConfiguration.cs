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

        // Filtered unique index — one user can be at most one leader, but
        // many leaders carry no UserId (guest speakers, pastors emeritus, etc.).
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        // Intentionally not temporal (per VERSIONING.md §2).
    }
}
