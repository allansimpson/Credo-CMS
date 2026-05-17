using CredoCms.Api;
using CredoCms.Api.Caching;
using CredoCms.Api.Composition;
using CredoCms.Api.Hubs;
using CredoCms.Api.Middleware;
using CredoCms.Application;
using CredoCms.Infrastructure;
using FluentValidation.AspNetCore;
using Serilog;

var bootstrapLogger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();
Log.Logger = bootstrapLogger;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, _, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration);
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.AddCookieAndOAuthAuthentication();
    builder.Services.AddAuthorizationPolicies();
    builder.Services.AddApiRateLimiting();
    builder.AddSignalRWithRealtimeNotifier();
    builder.ValidateProductionConfiguration();

    builder.Services
        .AddControllers(mvc =>
        {
            // Preserve the "Async" suffix in resolved action names so that
            // nameof(GetAsync) (used by every admin controller's CreatedAtAction
            // for its 201 Location header) actually matches the registered route.
            mvc.SuppressAsyncSuffixInActionNames = false;
        })
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();

    // In-memory output cache; default policy *does not* cache. Endpoints opt
    // in explicitly with [OutputCache(...)]. Admin and auth surfaces are
    // belt-and-braces blocked even if an attribute is added by mistake.
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(b => b
            .AddPolicy<MemberAuthVaryPolicy>()
            .NoCache());
        options.AddPolicy("MembersAuthVary", b => b
            .AddPolicy<MemberAuthVaryPolicy>());
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    if (builder.Environment.IsDevelopment())
    {
        var origins = builder.Configuration
            .GetSection("Authentication:Cors:DevAllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173"];

        builder.Services.AddCors(o =>
        {
            o.AddDefaultPolicy(p => p
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });
    }

    var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(aiConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(o =>
            o.ConnectionString = aiConnectionString);
    }

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // 403 → 404 for /api/admin/* and /api/docs/* must run between authentication
    // (which knows the user's roles) and the controller dispatch.
    app.UseAuthentication();
    app.UseRouting();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.UseOutputCache();

    app.UseMiddleware<ForbiddenToNotFoundMiddleware>();
    app.UseMiddleware<SessionExpiryHeaderMiddleware>();

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");

    // SPA fallback: any unmatched non-API request returns the SPA's index.html.
    // The file-not-found fallback handles the case where the SPA has not yet
    // been built into wwwroot.
    app.MapFallbackToFile("index.html");

    await app.MigrateAndSeedAsync();

    Log.Information("Credo CMS API starting up");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Credo CMS API terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

namespace CredoCms.Api
{
    /// <summary>Centralized authorization-policy names.</summary>
    public static class AuthorizationPolicies
    {
        public const string AdministratorOnly = "AdministratorOnly";
        public const string AdminShell = "AdminShell";
        public const string AnyAuthenticated = "AnyAuthenticated";
    }

    /// <summary>Marker so WebApplicationFactory&lt;Program&gt; can find this entrypoint.</summary>
    public partial class Program { }
}
