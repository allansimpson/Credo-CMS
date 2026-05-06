using CredoCms.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("SiteSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Phase 4 JSON / nvarchar(max) properties.
        builder.Property(x => x.ClassAudienceAgeGroupsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.BlogCategoriesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ProfanityWordlist).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ProfanityAllowlist).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ConnectCardInterestsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ConnectCardAcknowledgmentMessageJson).HasColumnType("nvarchar(max)");
    }
}
