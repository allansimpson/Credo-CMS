using CredoCms.Domain.ConnectCard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class ConnectCardSubmissionConfiguration : IEntityTypeConfiguration<ConnectCardSubmission>
{
    public void Configure(EntityTypeBuilder<ConnectCardSubmission> builder)
    {
        builder.ToTable("ConnectCardSubmissions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SubmittedAt);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IpAddressHash);
        builder.Property(x => x.AdminNotes).HasColumnType("nvarchar(max)");
        builder.Property(x => x.InterestCheckboxesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasConversion<int>();
        builder.AsTemporal("ConnectCardSubmissions");
    }
}
