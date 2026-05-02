using CredoCms.Domain.Auditing;
using CredoCms.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.EntityType);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.DetailsJson).HasColumnType("nvarchar(max)");

        // FK to ApplicationUser; SetNull lets historical entries survive a hard delete
        // with the snapshot display name still identifying the actor.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
