using CredoCms.Domain.Volunteers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class EventVolunteerRoleConfiguration : IEntityTypeConfiguration<EventVolunteerRole>
{
    public void Configure(EntityTypeBuilder<EventVolunteerRole> builder)
    {
        builder.ToTable("EventVolunteerRoles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => new { x.EventId, x.DisplayOrder });
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("EventVolunteerRoles");
    }
}

internal sealed class EventVolunteerSignupConfiguration : IEntityTypeConfiguration<EventVolunteerSignup>
{
    public void Configure(EntityTypeBuilder<EventVolunteerSignup> builder)
    {
        builder.ToTable("EventVolunteerSignups");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EventVolunteerRoleId, x.OccurrenceDate });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.EventId, x.OccurrenceDate });
        // Filtered unique: only enforced on active (uncanceled) signups so a
        // member can re-sign-up after canceling.
        builder.HasIndex(x => new { x.EventVolunteerRoleId, x.OccurrenceDate, x.UserId })
            .IsUnique()
            .HasFilter("[CanceledAt] IS NULL");
    }
}
