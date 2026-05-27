using CredoCms.Domain.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TemplateKey).IsUnique();
        builder.Property(x => x.HtmlBody).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PlainTextBody).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AvailableMergeFieldsJson).HasColumnType("nvarchar(max)");
        builder.AsTemporal("EmailTemplates");
    }
}
