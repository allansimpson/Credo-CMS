using CredoCms.Domain.Prayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class PrayerRequestConfiguration : IEntityTypeConfiguration<PrayerRequest>
{
    public void Configure(EntityTypeBuilder<PrayerRequest> builder)
    {
        builder.ToTable("PrayerRequests");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SubmittedByUserId);
        builder.HasIndex(x => x.CreatedAt);
        builder.Property(x => x.BodyJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("PrayerRequests");
    }
}

internal sealed class PrayerRequestUpdateConfiguration : IEntityTypeConfiguration<PrayerRequestUpdate>
{
    public void Configure(EntityTypeBuilder<PrayerRequestUpdate> builder)
    {
        builder.ToTable("PrayerRequestUpdates");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PrayerRequestId);
        builder.Property(x => x.BodyJson).HasColumnType("nvarchar(max)");
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("PrayerRequestUpdates");
    }
}

internal sealed class PrayerRequestPrayedForConfiguration : IEntityTypeConfiguration<PrayerRequestPrayedFor>
{
    public void Configure(EntityTypeBuilder<PrayerRequestPrayedFor> builder)
    {
        builder.ToTable("PrayerRequestPrayedFor");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PrayerRequestId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
