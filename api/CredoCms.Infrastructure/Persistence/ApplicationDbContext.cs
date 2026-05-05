using CredoCms.Application.Common;
using CredoCms.Domain.Announcements;
using CredoCms.Domain.Auditing;
using CredoCms.Domain.Documents;
using CredoCms.Domain.Events;
using CredoCms.Domain.Identity;
using CredoCms.Domain.Leaders;
using CredoCms.Domain.News;
using CredoCms.Domain.Pages;
using CredoCms.Domain.Scripture;
using CredoCms.Domain.Search;
using CredoCms.Domain.Sermons;
using CredoCms.Domain.Services;
using CredoCms.Domain.Settings;
using CredoCms.Domain.Tags;
using Microsoft.AspNetCore.Identity;
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
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<NewsItem> News => Set<NewsItem>();
    public DbSet<ServiceTime> ServiceTimes => Set<ServiceTime>();
    public DbSet<Leader> Leaders => Set<Leader>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AnnouncementBanner> AnnouncementBanner => Set<AnnouncementBanner>();
    public DbSet<SearchIndexEntry> SearchIndex => Set<SearchIndexEntry>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ScriptureReference> ScriptureReferences => Set<ScriptureReference>();
    public DbSet<SermonSeries> SermonSeries => Set<SermonSeries>();
    public DbSet<Sermon> Sermons => Set<Sermon>();
    public DbSet<SermonTag> SermonTags => Set<SermonTag>();
    public DbSet<SermonAttachment> SermonAttachments => Set<SermonAttachment>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRecurrenceException> EventRecurrenceExceptions => Set<EventRecurrenceException>();
    public DbSet<EventOccurrenceOverride> EventOccurrenceOverrides => Set<EventOccurrenceOverride>();
    public DbSet<EventRegistrationField> EventRegistrationFields => Set<EventRegistrationField>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<CalendarFeedToken> CalendarFeedTokens => Set<CalendarFeedToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Drop the "AspNet" prefix from every Identity table so the schema reads
        // cleanly alongside our domain tables. Migration RenameIdentityTables
        // emits RenameTable operations preserving data + FKs.
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }
}
