using CredoCms.Domain.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class EmailBroadcastConfiguration : IEntityTypeConfiguration<EmailBroadcast>
{
    public void Configure(EntityTypeBuilder<EmailBroadcast> builder)
    {
        builder.ToTable("EmailBroadcasts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ScheduledSendAt);
        builder.HasIndex(x => x.SentAt);
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PlainTextBody).HasColumnType("nvarchar(max)");
        builder.Property(x => x.TargetGroupIdsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.FailureReason).HasColumnType("nvarchar(2000)");
        builder.Property(x => x.TargetMode).HasConversion<int>();
        builder.Property(x => x.SendMode).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Category).HasConversion<int>();
        builder.AsTemporal("EmailBroadcasts");
    }
}

internal sealed class EmailBroadcastRecipientConfiguration : IEntityTypeConfiguration<EmailBroadcastRecipient>
{
    public void Configure(EntityTypeBuilder<EmailBroadcastRecipient> builder)
    {
        builder.ToTable("EmailBroadcastRecipients");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.BroadcastId, x.Status });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SendGridMessageId);
        builder.Property(x => x.BounceReason).HasColumnType("nvarchar(1000)");
        builder.Property(x => x.Status).HasConversion<int>();
    }
}
