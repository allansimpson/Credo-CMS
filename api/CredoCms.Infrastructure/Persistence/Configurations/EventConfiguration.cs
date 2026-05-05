using CredoCms.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", t => t.IsTemporal(temp =>
        {
            temp.HasPeriodStart("ValidFrom");
            temp.HasPeriodEnd("ValidTo");
            temp.UseHistoryTable("EventsHistory");
        }));
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.StartsAt);
        builder.Property(x => x.DescriptionJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.RegistrationConfirmationMessageJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Visibility).HasConversion<int>();
        builder.Property(x => x.RegistrationMode).HasConversion<int>();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class EventRecurrenceExceptionConfiguration : IEntityTypeConfiguration<EventRecurrenceException>
{
    public void Configure(EntityTypeBuilder<EventRecurrenceException> builder)
    {
        builder.ToTable("EventRecurrenceExceptions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EventId, x.OccurrenceDate }).IsUnique();
    }
}

internal sealed class EventOccurrenceOverrideConfiguration : IEntityTypeConfiguration<EventOccurrenceOverride>
{
    public void Configure(EntityTypeBuilder<EventOccurrenceOverride> builder)
    {
        builder.ToTable("EventOccurrenceOverrides");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EventId, x.OriginalOccurrenceDate }).IsUnique();
        builder.Property(x => x.OverrideDescriptionJson).HasColumnType("nvarchar(max)");
    }
}
