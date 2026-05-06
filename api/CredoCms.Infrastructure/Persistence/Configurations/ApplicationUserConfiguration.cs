using CredoCms.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CredoCms.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.RequirePasswordChangeOnFirstLogin).HasDefaultValue(false);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // Phase 4: directory + notification preference defaults.
        builder.Property(u => u.IsListedInDirectory).HasDefaultValue(false);
        builder.Property(u => u.ShowEmailInDirectory).HasDefaultValue(false);
        builder.Property(u => u.ShowPhoneInDirectory).HasDefaultValue(false);
        builder.Property(u => u.ShowAddressInDirectory).HasDefaultValue(false);
        builder.Property(u => u.ShowPhotoInDirectory).HasDefaultValue(false);
        builder.Property(u => u.ReceiveNewsEmails).HasDefaultValue(true);
        builder.Property(u => u.ReceiveBlogEmails).HasDefaultValue(false);
        builder.Property(u => u.ReceiveBroadcastEmails).HasDefaultValue(true);
        builder.Property(u => u.ReceiveGroupEmailsGlobal).HasDefaultValue(true);
        builder.Property(u => u.PublicAuthorBio).HasColumnType("nvarchar(max)");

        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.IsListedInDirectory);
    }
}
