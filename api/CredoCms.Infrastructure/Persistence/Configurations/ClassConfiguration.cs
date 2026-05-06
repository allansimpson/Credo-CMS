using CredoCms.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class ClassSlotConfiguration : IEntityTypeConfiguration<ClassSlot>
{
    public void Configure(EntityTypeBuilder<ClassSlot> builder)
    {
        builder.ToTable("ClassSlots");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.AudienceAgeGroup);
        builder.Property(x => x.DescriptionJson).HasColumnType("nvarchar(max)");
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("ClassSlots");
    }
}

internal sealed class ClassOfferingConfiguration : IEntityTypeConfiguration<ClassOffering>
{
    public void Configure(EntityTypeBuilder<ClassOffering> builder)
    {
        builder.ToTable("ClassOfferings");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ClassSlotId);
        builder.HasIndex(x => new { x.StartDate, x.EndDate });
        builder.Property(x => x.DescriptionJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.DetailedScheduleJson).HasColumnType("nvarchar(max)");
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("ClassOfferings");
    }
}
