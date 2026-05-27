using CredoCms.Domain.Announcements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class AnnouncementBannerConfiguration : IEntityTypeConfiguration<AnnouncementBanner>
{
    public void Configure(EntityTypeBuilder<AnnouncementBanner> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Severity).HasConversion<int>();
        builder.AsTemporal("AnnouncementBanner");
    }
}
