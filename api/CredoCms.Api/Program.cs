using System.Threading.RateLimiting;
using CredoCms.Api.Caching;
using CredoCms.Api.Hubs;
using CredoCms.Api.Middleware;
using CredoCms.Application;
using CredoCms.Domain.Common;
using CredoCms.Infrastructure;
using CredoCms.Infrastructure.Configuration;
using CredoCms.Infrastructure.Persistence;
using CredoCms.Infrastructure.Seeding;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using CredoCms.Api;

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

    // -- Application + Infrastructure DI --------------------------------------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // -- Cookie auth options --------------------------------------------------
    var cookieOptions = builder.Configuration
        .GetSection(CookieAuthOptions.SectionName)
        .Get<CookieAuthOptions>() ?? new CookieAuthOptions();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.Name = cookieOptions.Name;
        if (!string.IsNullOrWhiteSpace(cookieOptions.Domain))
        {
            options.Cookie.Domain = cookieOptions.Domain;
        }
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;

        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/404";

        // API responses must not redirect; return 401/403 status codes instead.
        options.Events.OnRedirectToLogin = context =>
        {
            if (IsApiPath(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (IsApiPath(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        static bool IsApiPath(HttpRequest request) =>
            request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
    });

    // -- Authorization policies -----------------------------------------------
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicies.AdministratorOnly, p =>
            p.RequireRole(SystemConstants.Roles.Administrator));
        options.AddPolicy(AuthorizationPolicies.AdminShell, p =>
            p.RequireRole(SystemConstants.Roles.AdminShellRoles.ToArray()));
        options.AddPolicy(AuthorizationPolicies.AnyAuthenticated, p =>
            p.RequireAuthenticatedUser());
    });

    // -- Rate limiting --------------------------------------------------------
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

        options.AddPolicy("forgot-password", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                }));

        options.AddPolicy("reset-password", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                }));

        // Event registration: 5 submissions per IP per 10 minutes.
        // Honeypot + time-to-submit defenses also apply server-side.
        options.AddPolicy("event-register", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                }));

        // Connect card: 5 submissions per IP per hour. Sliding window so a
        // burst right before the hour boundary doesn't reset cleanly. Plus
        // Turnstile + honeypot + 5s time-to-submit at the service layer.
        options.AddPolicy(
            CredoCms.Api.Controllers.PublicConnectCardController.RateLimitPolicy,
            httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                    }));
    });

    // -- MVC + Validation -----------------------------------------------------
    builder.Services
        .AddControllers(mvc =>
        {
            // Preserve the "Async" suffix in resolved action names so that
            // nameof(GetAsync) (used by every admin controller's CreatedAtAction
            // for its 201 Location header) actually matches the registered route.
            // ASP.NET Core strips the suffix by default, which makes the URL
            // helper throw "No route matches the supplied values" when computing
            // the Location header on Create endpoints.
            mvc.SuppressAsyncSuffixInActionNames = false;
        })
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();

    // -- Output cache ---------------------------------------------------------
    // In-memory store; default policy *does not* cache. Endpoints opt in
    // explicitly with [OutputCache(...)]. Admin and auth surfaces are
    // belt-and-braces blocked even if an attribute is added by mistake.
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(b => b
            .AddPolicy<MemberAuthVaryPolicy>()
            .NoCache());
        options.AddPolicy("MembersAuthVary", b => b
            .AddPolicy<MemberAuthVaryPolicy>());
    });

    // -- OpenAPI --------------------------------------------------------------
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // -- SignalR --------------------------------------------------------------
    var signalRBuilder = builder.Services.AddSignalR();
    var azureSignalR = builder.Configuration.GetConnectionString("AzureSignalR");
    if (!string.IsNullOrWhiteSpace(azureSignalR))
    {
        signalRBuilder.AddAzureSignalR(azureSignalR);
    }
    builder.Services.AddSingleton<CredoCms.Application.RealTime.IRealtimeNotifier,
        CredoCms.Api.RealTime.SignalRRealtimeNotifier>();

    // -- CORS for dev SPA -----------------------------------------------------
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

    // -- ApplicationInsights (optional) ---------------------------------------
    var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(aiConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(o =>
            o.ConnectionString = aiConnectionString);
    }

    var app = builder.Build();

    // -- Middleware pipeline --------------------------------------------------
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

    // After Authorization but before MapControllers — turns Forbidden into NotFound
    // for the protected admin/docs URL prefixes only.
    app.UseMiddleware<ForbiddenToNotFoundMiddleware>();

    // After auth/authz: stamp X-Session-Expires-At on authenticated 2xx responses.
    app.UseMiddleware<SessionExpiryHeaderMiddleware>();

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");

    // SPA fallback: any unmatched non-API request returns the SPA's index.html.
    // Phase 1 has no built SPA yet; the file-not-found fallback gracefully handles
    // that case until the SPA is built into wwwroot.
    app.MapFallbackToFile("index.html");

    // -- Database setup -------------------------------------------------------
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (app.Environment.IsDevelopment() &&
            !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
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

        // Seed only when the database is reachable.
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
