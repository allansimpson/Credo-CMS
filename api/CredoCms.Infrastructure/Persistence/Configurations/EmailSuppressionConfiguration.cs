using CredoCms.Domain.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class EmailSuppressionConfiguration : IEntityTypeConfiguration<EmailSuppression>
{
    public void Configure(EntityTypeBuilder<EmailSuppression> builder)
    {
        builder.ToTable("EmailSuppressions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EmailAddress).IsUnique();
        builder.HasIndex(x => x.SuppressionType);
        builder.HasIndex(x => x.CreatedAt);
        builder.Property(x => x.SuppressionType).HasConversion<int>();
        builder.Property(x => x.CreatedSource).HasConversion<int>();
    }
}
