using CredoCms.Infrastructure.Persistence;
using CredoCms.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CredoCms.Api.Composition;

internal static class DatabaseStartup
{
    /// <summary>
    /// Apply migrations (Dev only, when a connection string is configured)
    /// and run the seeder. Reachability failures are logged and the app
    /// continues without seed data so the API can still serve requests in
    /// environments where the DB is intermittently unavailable.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (app.Environment.IsDevelopment() &&
            !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Migration failed on startup; the database is unavailable. Continue without seeding.");
            }
        }

        try
        {
            if (await db.Database.CanConnectAsync())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                await seeder.SeedAsync();
            }
            else
            {
                Log.Warning("Database is not reachable; skipping seed.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Seed step failed; application will continue without seed data.");
        }
    }
}
