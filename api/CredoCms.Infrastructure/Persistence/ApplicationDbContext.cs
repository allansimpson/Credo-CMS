using CredoCms.Application.Common;
using CredoCms.Domain.Auditing;
using CredoCms.Domain.Identity;
using CredoCms.Domain.Settings;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="DbContext"/> backing the application.
///
/// Implements <see cref="IApplicationDbContext"/>, which is the only surface the
/// Application layer sees. All EF-specific concerns — DbSets, configurations,
/// temporal-table conventions, the versioning interceptor — are encapsulated here.
/// </summary>
public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Identity table renames — keep the AspNet* defaults but make our extra tables
        // use the same naming style.
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers");
        builder.Entity<ApplicationRole>().ToTable("AspNetRoles");
    }
}
