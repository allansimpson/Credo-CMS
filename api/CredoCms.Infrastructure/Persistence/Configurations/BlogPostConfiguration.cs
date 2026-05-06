using CredoCms.Domain.Blog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPosts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.AuthorUserId);
        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsPublished);
        builder.Property(x => x.BodyJson).HasColumnType("nvarchar(max)");
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.AsTemporal("BlogPosts");
    }
}

internal sealed class BlogPostTagConfiguration : IEntityTypeConfiguration<BlogPostTag>
{
    public void Configure(EntityTypeBuilder<BlogPostTag> builder)
    {
        builder.ToTable("BlogPostTags");
        builder.HasKey(x => new { x.BlogPostId, x.TagId });
        builder.HasIndex(x => x.TagId);
    }
}
