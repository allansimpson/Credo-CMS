using CredoCms.Domain.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.HasKey(x => x.Id);

        // Filtered unique index on Slug — the filter excludes soft-deleted rows so
        // a page can be re-created with the same slug after deletion.
        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(x => x.BodyJson).HasColumnType("nvarchar(max)");

        // Soft-delete query filter — Application reads only see non-deleted rows
        // by default. Restore + admin "deleted" tab use IgnoreQueryFilters().
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.AsTemporal("Pages");
    }
}
