using CredoCms.Domain.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class NewsItemConfiguration : IEntityTypeConfiguration<NewsItem>
{
    public void Configure(EntityTypeBuilder<NewsItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.AuthorUserId);

        builder.Property(x => x.BodyJson).HasColumnType("nvarchar(max)");

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.AsTemporal("News");
    }
}
