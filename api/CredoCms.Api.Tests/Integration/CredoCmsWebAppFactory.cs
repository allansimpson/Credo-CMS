using CredoCms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Boots the API in-process for integration tests, swapping the SQL Server DbContext
/// for the EF Core in-memory provider. The migration / seed step in Program.cs
/// detects the missing connection string and gracefully skips, leaving the test
/// to do its own setup.
/// </summary>
public sealed class CredoCmsWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"CredoCmsTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the SqlServer DbContext with an in-memory one.
            var dbDescriptor = services.Single(s => s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            services.Remove(dbDescriptor);

            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseInMemoryDatabase(_databaseName));

            // Ensure the IApplicationDbContext mapping continues to point at the new ctx.
            services.RemoveAll<CredoCms.Application.Common.IApplicationDbContext>();
            services.AddScoped<CredoCms.Application.Common.IApplicationDbContext>(
                sp => sp.GetRequiredService<ApplicationDbContext>());
        });
    }
}
