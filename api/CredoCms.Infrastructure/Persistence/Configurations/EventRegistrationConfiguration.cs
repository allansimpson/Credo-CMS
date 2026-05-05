using CredoCms.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class EventRegistrationFieldConfiguration : IEntityTypeConfiguration<EventRegistrationField>
{
    public void Configure(EntityTypeBuilder<EventRegistrationField> builder)
    {
        builder.ToTable("EventRegistrationFields");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EventId, x.DisplayOrder });
        builder.Property(x => x.FieldType).HasConversion<int>();
        builder.Property(x => x.OptionsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NumberMin).HasColumnType("decimal(18,4)");
        builder.Property(x => x.NumberMax).HasColumnType("decimal(18,4)");
    }
}

internal sealed class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.ToTable("EventRegistrations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SubmitterEmail);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.FieldValuesJson).HasColumnType("nvarchar(max)");
    }
}
