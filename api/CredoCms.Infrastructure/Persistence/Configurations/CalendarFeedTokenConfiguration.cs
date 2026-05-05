using CredoCms.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class CalendarFeedTokenConfiguration : IEntityTypeConfiguration<CalendarFeedToken>
{
    public void Configure(EntityTypeBuilder<CalendarFeedToken> builder)
    {
        builder.ToTable("CalendarFeedTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
