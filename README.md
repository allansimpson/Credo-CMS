# Credo CMS

A custom church website and content management system, architected for a single church
today and multi-tenant SaaS in a future phase.

> **Phase 1 — Foundation.** This branch contains the foundational infrastructure: the
> ASP.NET Core 10 / EF Core / Identity backend, the React + Vite + Tailwind + shadcn
> SPA shell, theming, role-based routing, audit log, versioning infrastructure, SignalR,
> and deployment templates. Content types (Pages, News, Sermons, Events, Members,
> Groups, etc.) arrive in Phases 2–5.

---

## Repository Layout

```
/credo-cms/
  /api/                          ASP.NET Core 10 solution
    CredoCms.sln
    /CredoCms.Api/               controllers, middleware, Program.cs
    /CredoCms.Application/       services, DTOs, validators, interfaces
    /CredoCms.Domain/            entities, domain interfaces, enums
    /CredoCms.Infrastructure/    EF Core DbContext, migrations, external services
    /CredoCms.*.Tests/           xUnit + FluentAssertions + Moq
  /app/                          React + Vite + TypeScript SPA
  /deploy/                       Bicep templates, deployment guide
  /docs/                         Astro documentation site (placeholder; built in Phase 6)
  /.github/workflows/            CI/CD
  README.md
  BUILD_PLAN.md                  Phase 1 build plan
  IMPLEMENTATION_NOTES.md        Decisions, ambiguities, deviations log
  VERSIONING.md                  Temporal-table versioning design + restore semantics
  MULTI_TENANCY.md               Migration path to multi-tenant SaaS
  ROADMAP.md                     Features deferred to v1.x or later
  appsettings.template.json      Source of truth for required configuration
```

---

## Prerequisites

| Tool | Minimum | Notes |
|---|---|---|
| Visual Studio 2022 (17.12+) or 2024 | — | Primary IDE for the API solution |
| VS Code | latest | Primary IDE for the SPA |
| .NET SDK | **10.0** | `dotnet --version` should report 10.x |
| Node.js | **20+** | Validated against Node 22 |
| SQL Server | LocalDB / Express / full | Connection string in `appsettings.Development.json` |
| Azure CLI | latest | Required for deployment via Bicep |
| `dotnet-ef` global tool | matching SDK | `dotnet tool install --global dotnet-ef` |

---

## Quick Start

```bash
# 1. Clone
git clone https://github.com/allansimpson/Credo-CMS.git
cd Credo-CMS

# 2. Configure
#    Copy appsettings.template.json into api/CredoCms.Api/appsettings.Development.json
#    and fill in the required values (connection string, default admin credentials).

# 3. Build the API
cd api
dotnet build

# 4. Apply migrations & seed
dotnet ef database update --project CredoCms.Infrastructure --startup-project CredoCms.Api

# 5. Run the API (also serves the SPA from wwwroot once it's built)
dotnet run --project CredoCms.Api

# 6. In a second terminal, run the SPA in dev mode
cd ../app
npm install
npm run dev
```

The API listens on `https://localhost:5001` by default; the Vite dev server proxies
`/api/*` to it. Once authenticated, the church-themed public pages render at `/` and
the system-themed admin shell at `/admin`.

**Default Administrator credentials** are seeded from `Identity:DefaultAdminEmail` and
`Identity:DefaultAdminPassword` in configuration. The seeded admin account has
`RequirePasswordChangeOnFirstLogin = true`, so the first sign-in forces a password
change before any other action is permitted. **Always change these in production
before opening the site to the public.**

---

## Architecture Summary

Four-project layered solution:

| Project | Depends on | Contains |
|---|---|---|
| `CredoCms.Domain` | (nothing) | Entities, value objects, enums, domain interfaces (`IVersionedEntity`) |
| `CredoCms.Application` | `Domain` | Services, DTOs, validators, abstractions (`IApplicationDbContext`, `ICurrentUserService`, `IAuditLogger`, `IEmailService`) |
| `CredoCms.Infrastructure` | `Application`, `Domain` | EF Core `DbContext`, migrations, interceptors, external service implementations |
| `CredoCms.Api` | `Application`, `Infrastructure` | Controllers, middleware, `Program.cs`, DI composition, SignalR hubs |

**Critical rule:** `Application` does **not** reference `Infrastructure` or EF Core. The
`Application` layer is database-technology-agnostic by design.

The SPA is a Vite + React + TypeScript app served either from the API's `wwwroot/` (the
default Phase 1 deployment shape) or as an Azure Static Web App (alternative; see
`/deploy/README.md`).

---

## Documentation Index

- [`BUILD_PLAN.md`](./BUILD_PLAN.md) — Phase 1 ordered build plan (informational; the
  plan that produced this branch).
- [`IMPLEMENTATION_NOTES.md`](./IMPLEMENTATION_NOTES.md) — running log of non-trivial
  decisions, ambiguities encountered, and deviations from the prompt.
- [`VERSIONING.md`](./VERSIONING.md) — temporal-table configuration, `IVersionedEntity`
  pattern, restore semantics, destructive-migration warning.
- [`MULTI_TENANCY.md`](./MULTI_TENANCY.md) — what's already designed for multi-tenant,
  what's deferred, how to refactor incrementally.
- [`ROADMAP.md`](./ROADMAP.md) — features deferred to v1.x or later.
- [`/deploy/README.md`](./deploy/README.md) — Azure deployment via Bicep, custom domain
  configuration, sizing & cost guidance, alternative SPA hosting via Static Web Apps.

---

## Help & Feedback

For issues with Phase 1 itself, open an issue on the GitHub repository. For features
on the deferred list, see `ROADMAP.md`.

---

## Phase 5 — Email & Communications setup

### Picking a provider

Site Settings → `EmailProvider`:

- **None** (default) — no outbound mail. `LoggingEmailService` writes
  every send attempt to Serilog. Use this until provider config is
  verified.
- **SendGrid** — free tier covers 100/day. Set `SendGridApiKey`
  (full-access key with Mail Send + Inbound Parse permissions) and
  `SendGridWebhookSecret` (the ECDSA public key from SendGrid's
  Mail Settings → Event Webhook screen).
- **SMTP** — generic SMTP relay (Postmark, Mailgun, your ISP). Set
  `SmtpHost`, `SmtpPort` (587 for STARTTLS, 465 for SslOnConnect),
  `SmtpUsername`, `SmtpPassword`, `SmtpUseSsl`.

After picking + saving, flip `EmailEnabled` to `true`. Until that
flag is on, every send is a no-op (logs only) — production-safe
default.

### SendGrid webhook configuration

1. SendGrid console → Settings → Mail Settings → **Event Webhook**.
2. HTTP Post URL: `https://<your-host>/api/webhooks/sendgrid`.
3. Enable **Signed Event Webhook**; copy the public key into
   Site Settings → `SendGridWebhookSecret`.
4. Subscribe to: delivered, open, click, bounce, dropped,
   spam_report, unsubscribe, group_unsubscribe.

### Test send + suppression list

`POST /api/admin/site-settings/test-email` (Administrator) accepts the
in-flight provider config and dispatches a one-shot message — verify
delivery before flipping `EmailEnabled` on. Hard bounces + spam reports
+ one-click unsubscribes auto-write to the suppression list; manual
remove warns about CAN-SPAM compliance.

### Broadcasts

`/admin/broadcasts/new` — compose, target (All Members / specific
groups), schedule or send now. Use `{{firstName}}`, `{{lastName}}`,
`{{unsubscribeUrl}}` as merge fields. Recipients resolved at send
time so membership changes between compose and dispatch are picked
up. Each recipient gets a per-user HMAC-signed unsubscribe URL.

### Volunteer signups

`/admin/events/{id}/volunteer-roles` — define roles with slot counts.
Members sign up via the event detail page; 24-48h reminders fire
automatically.

### Background workers

API hosts four Phase 5 BackgroundServices:

- `BroadcastSendWorker` — 15s tick.
- `ScheduledPublishingService` — 60s tick (News + Blog auto-publish).
- `AdminNotificationDigestService` — 5min tick.
- `EventVolunteerReminderService` — 1h tick.

Single-instance App Service is the v1 deployment target.
