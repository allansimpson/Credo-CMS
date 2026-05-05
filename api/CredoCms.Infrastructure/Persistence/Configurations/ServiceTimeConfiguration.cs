using CredoCms.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class ServiceTimeConfiguration : IEntityTypeConfiguration<ServiceTime>
{
    public void Configure(EntityTypeBuilder<ServiceTime> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DayOfWeek, x.DisplayOrder });
        builder.Property(x => x.DayOfWeek).HasConversion<int>();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("ServiceTimes");
    }
}
