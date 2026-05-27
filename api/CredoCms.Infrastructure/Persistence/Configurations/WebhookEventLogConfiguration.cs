using CredoCms.Domain.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class WebhookEventLogConfiguration : IEntityTypeConfiguration<WebhookEventLog>
{
    public void Configure(EntityTypeBuilder<WebhookEventLog> builder)
    {
        builder.ToTable("WebhookEventLog");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => x.ProcessedAt);
    }
}

internal sealed class AdminNotificationLastSentConfiguration : IEntityTypeConfiguration<AdminNotificationLastSent>
{
    public void Configure(EntityTypeBuilder<AdminNotificationLastSent> builder)
    {
        builder.ToTable("AdminNotificationLastSent");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.NotificationCategory }).IsUnique();
        builder.Property(x => x.NotificationCategory).HasConversion<int>();
    }
}
