using CredoCms.Application.Common;
using CredoCms.Domain.Common;
using CredoCms.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace CredoCms.Infrastructure.Tests.Persistence;

public sealed class VersioningInterceptorTests
{
    /// <summary>
    /// A small entity that implements IVersionedEntity but isn't a real domain
    /// type — keeps the test focused on the interceptor's behavior, independent
    /// of EF temporal-table support (which the in-memory provider lacks).
    /// </summary>
    private sealed class StubVersionedEntity : IVersionedEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? ModifiedByUserId { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
    }

    private sealed class StubDbContext : DbContext
    {
        public DbSet<StubVersionedEntity> Items => Set<StubVersionedEntity>();

        public StubDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb) =>
            mb.Entity<StubVersionedEntity>().ToTable("Stubs");
    }

    private static StubDbContext NewContext(VersioningInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        return new StubDbContext(options);
    }

    [Fact]
    public async Task Stamps_ModifiedByUserId_and_ModifiedAt_on_added_entity()
    {
        var actingUserId = Guid.NewGuid();
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UserId).Returns(actingUserId);

        var interceptor = new VersioningInterceptor(current.Object);
        await using var db = NewContext(interceptor);

        var entity = new StubVersionedEntity { Name = "first" };
        db.Items.Add(entity);
        await db.SaveChangesAsync();

        entity.ModifiedByUserId.Should().Be(actingUserId);
        entity.ModifiedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Stamps_on_modified_entity()
    {
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UserId).Returns(firstUser);

        var interceptor = new VersioningInterceptor(current.Object);
        await using var db = NewContext(interceptor);

        var entity = new StubVersionedEntity { Name = "first" };
        db.Items.Add(entity);
        await db.SaveChangesAsync();

        current.SetupGet(x => x.UserId).Returns(secondUser);
        entity.Name = "second";
        await db.SaveChangesAsync();

        entity.ModifiedByUserId.Should().Be(secondUser);
    }

    [Fact]
    public async Task Falls_back_to_SystemUserId_when_current_user_is_unknown()
    {
        var current = new Mock<ICurrentUserService>();
        // The infrastructure CurrentUserService falls back to SystemUserId; mirror that here.
        current.SetupGet(x => x.UserId).Returns(SystemConstants.SystemUserId);

        var interceptor = new VersioningInterceptor(current.Object);
        await using var db = NewContext(interceptor);

        var entity = new StubVersionedEntity { Name = "system-write" };
        db.Items.Add(entity);
        await db.SaveChangesAsync();

        entity.ModifiedByUserId.Should().Be(SystemConstants.SystemUserId);
    }
}
