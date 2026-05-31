using CredoCms.Domain.Identity;
using CredoCms.Infrastructure.Identity;
using CredoCms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CredoCms.Infrastructure.Composition;

internal static class IdentityComposition
{
    public static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Password policy ───────────────────────────────────────────────────
        // Aligned with NIST SP 800-63B (current revision) — length is the
        // dominant control, composition rules push users toward predictable
        // patterns ("Password1!"), and the real security teeth is breach-
        // corpus screening via HibpPasswordValidator below. We keep an above-
        // baseline 14-char minimum and ditch the upper/lower/digit/symbol
        // gates entirely so a long passphrase ("correct horse battery staple")
        // is accepted on its own merits.
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 14;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 4;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedAccount = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<HibpPasswordValidator>();

        // ── Hash work factor ──────────────────────────────────────────────────
        // .NET's default is 100,000 PBKDF2 iterations. OWASP's 2023+ guidance
        // for PBKDF2-HMAC-SHA512 (Identity's v3 format) is 210,000+; bumping
        // to 600,000 keeps us comfortably ahead. The hash format carries a
        // version byte so existing user hashes are unaffected — they'll
        // silently re-hash under the new iteration count on next login.
        services.Configure<PasswordHasherOptions>(o =>
        {
            o.IterationCount = 600_000;
            o.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
        });

        services.Configure<SecurityStampValidatorOptions>(o =>
        {
            // Short interval so administrative force-logout takes effect quickly.
            o.ValidationInterval = TimeSpan.FromMinutes(1);
        });

        // ── HIBP breach-corpus screening ──────────────────────────────────────
        // The big modern win. Catches "Password reused from some prior breach"
        // — the actual failure mode credential-stuffing attacks exploit, which
        // composition rules don't address. Uses k-anonymity (only the first
        // 5 chars of the password's SHA-1 leave the server), so the password
        // itself is never transmitted.
        services.AddOptions<HibpPasswordValidatorOptions>()
            .Bind(configuration.GetSection(HibpPasswordValidatorOptions.SectionName));

        services.AddHttpClient(HibpPasswordValidator.HttpClientName, c =>
        {
            c.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
            c.Timeout = TimeSpan.FromSeconds(5);
            // HIBP requires a non-empty User-Agent on all requests.
            c.DefaultRequestHeaders.UserAgent.ParseAdd("CredoCMS-Auth/1.0");
        });

        return services;
    }
}
