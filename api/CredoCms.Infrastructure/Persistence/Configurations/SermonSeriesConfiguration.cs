using CredoCms.Domain.Sermons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class SermonSeriesConfiguration : IEntityTypeConfiguration<SermonSeries>
{
    public void Configure(EntityTypeBuilder<SermonSeries> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.Property(x => x.DescriptionJson).HasColumnType("nvarchar(max)");

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.AsTemporal("SermonSeries");
    }
}
