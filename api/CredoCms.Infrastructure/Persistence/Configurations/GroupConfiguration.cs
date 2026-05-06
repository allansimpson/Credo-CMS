using CredoCms.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.Visibility);
        builder.HasIndex(x => x.IsActive);
        builder.Property(x => x.DescriptionJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Visibility).HasConversion<int>();
        builder.Property(x => x.Joinability).HasConversion<int>();
        builder.Property(x => x.RequiresMessageOnJoinRequest).HasConversion<int>();
        builder.Property(x => x.RosterVisibility).HasConversion<int>();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("Groups");
    }
}

internal sealed class GroupMembershipConfiguration : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> builder)
    {
        builder.ToTable("GroupMemberships");
        builder.HasKey(x => x.Id);
        // Active and Pending are the "live" statuses — at most one of those per
        // (GroupId, UserId). Removed/Declined rows are historical and can coexist.
        builder.HasIndex(x => new { x.GroupId, x.UserId, x.Status });
        builder.HasIndex(x => new { x.GroupId, x.IsLeader });
        builder.HasIndex(x => x.UserId);
        builder.Property(x => x.Status).HasConversion<int>();
    }
}
