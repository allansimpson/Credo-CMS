using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CredoCms.Infrastructure.Persistence;

/// <summary>
/// Used by the <c>dotnet ef</c> CLI to construct a DbContext at design time
/// (e.g., when generating migrations). The connection string is intentionally a
/// placeholder — design-time tooling does not actually open the connection unless
/// running <c>database update</c>, in which case the real connection string from
/// configuration takes over via the API's runtime DI registration.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                Environment.GetEnvironmentVariable("CREDOCMS_DESIGNTIME_CONNECTION")
                    ?? "Server=(localdb)\\MSSQLLocalDB;Database=CredoCmsDesignTime;Trusted_Connection=True;TrustServerCertificate=True;",
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
