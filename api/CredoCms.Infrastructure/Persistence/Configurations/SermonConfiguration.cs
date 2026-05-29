using CredoCms.Domain.Sermons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class SermonConfiguration : IEntityTypeConfiguration<Sermon>
{
    public void Configure(EntityTypeBuilder<Sermon> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.YouTubeVideoId).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.SermonSeriesId);
        builder.HasIndex(x => x.PublishedAt);
        builder.Property(x => x.DescriptionJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Transcript).HasColumnType("nvarchar(max)");
        builder.Property(x => x.TranscriptSource).HasConversion<int>();
        builder.Property(x => x.ServiceType).HasConversion<int>();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("Sermons");
    }
}

internal sealed class SermonTagConfiguration : IEntityTypeConfiguration<SermonTag>
{
    public void Configure(EntityTypeBuilder<SermonTag> builder)
    {
        builder.ToTable("SermonTags");
        builder.HasKey(x => new { x.SermonId, x.TagId });
        builder.HasIndex(x => x.TagId);
    }
}

internal sealed class SermonAttachmentConfiguration : IEntityTypeConfiguration<SermonAttachment>
{
    public void Configure(EntityTypeBuilder<SermonAttachment> builder)
    {
        builder.ToTable("SermonAttachments");
        builder.HasKey(x => new { x.SermonId, x.DocumentId });
    }
}
