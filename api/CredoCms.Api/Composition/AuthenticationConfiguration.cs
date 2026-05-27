using CredoCms.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CredoCms.Api.Composition;

internal static class AuthenticationConfiguration
{
    public static void AddCookieAndOAuthAuthentication(this WebApplicationBuilder builder)
    {
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
        });

        // Facebook OAuth: app id and secret bound from configuration so a real
        // instance can run end-to-end while leaving credentials out of source.
        // The handler is always added so the auth scheme exists; whether the SPA
        // shows the "Continue with Facebook" button is governed at runtime by
        // SiteSettings.FacebookLoginEnabled.
        var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
        var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
        if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
        {
            builder.Services
                .AddAuthentication()
                .AddFacebook(options =>
                {
                    options.AppId = facebookAppId;
                    options.AppSecret = facebookAppSecret;
                    options.SaveTokens = true;
                });
        }
    }

    private static bool IsApiPath(HttpRequest request) =>
        request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
        request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
}
