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

---

## Phase 6 — Operator runbook

The Astro docs site at `/docs/*` (Editor / Administrator only) covers
day-to-day operation in detail. This section is the engineer-facing
runbook for tasks that touch infrastructure, the database, or
production secrets.

### Quick start (developer)

Prerequisites:

- .NET 10 SDK
- Node 20+
- SQL Server LocalDB or full instance
- Azure CLI (for production deploy)

```sh
git clone <repo>
cd Credo-CMS

# Backend — restore + migrate + run
cd api
cp CredoCms.Api/appsettings.template.json CredoCms.Api/appsettings.Development.json
# Edit the connection string in appsettings.Development.json
dotnet ef database update --project CredoCms.Infrastructure --startup-project CredoCms.Api
dotnet run --project CredoCms.Api

# SPA (in another terminal)
cd ../app
npm install
npm run dev

# Astro docs site (optional)
cd ../docs
npm install
npm run build
```

The seeded admin email + password is in `appsettings.Development.json`
under `Identity:DefaultAdminEmail` / `DefaultAdminPassword`. Sign in,
change password immediately.

### Production deployment (Azure)

The `/deploy/` folder ships Bicep templates. High-level:

1. Create a resource group in your target Azure region.
2. Deploy `deploy/main.bicep` with the parameter file:
   ```sh
   az deployment group create \
     --resource-group <rg-name> \
     --template-file deploy/main.bicep \
     --parameters deploy/main.parameters.json
   ```
3. Resources provisioned: App Service (Linux, B1+), Azure SQL (S0+),
   Storage (Blob, Hot tier), Application Insights, optionally
   Key Vault.
4. Configure App Service application settings (Connection Strings +
   Identity + provider keys). See `appsettings.template.json` for the
   full key list.
5. Trigger the GitHub Actions deploy workflow (or push to main).
6. On first boot, the API runs migrations + seeds idempotently.
7. Sign in as the seeded admin, change password, configure Site Settings.

**Cost estimate at default tiers:** ~$50-100/month (B1 App Service ~$15,
SQL S0 ~$15, Storage ~$2, App Insights free tier, SendGrid free tier).

A required pre-launch acceptance test: deploy to a staging Azure
subscription end-to-end + smoke-test the deploy. See `deploy/README.md`.

### Operations runbook

#### Backup the database

Azure SQL has automatic point-in-time restore (7-35 day retention). For
off-site copies, run a monthly BACPAC export to Blob Storage:

```sh
az sql db export \
  --resource-group <rg> \
  --server <sql-server> \
  --name <db> \
  --storage-key <blob-key> \
  --storage-key-type StorageAccessKey \
  --storage-uri https://<storage>.blob.core.windows.net/backups/$(date +%Y%m%d).bacpac \
  --admin-user <user> \
  --admin-password <password>
```

#### Restore from backup

Point-in-time restore creates a new database. Update the App Service's
`ConnectionStrings__DefaultConnection` setting to point at the
restored database, then restart the App Service. The migration set
runs automatically on boot.

#### Roll out a database migration

```sh
cd api
dotnet ef migrations add <Name> --project CredoCms.Infrastructure --startup-project CredoCms.Api
# Review the generated migration; commit
# Push; the GH Actions deploy applies it automatically on first boot
```

For destructive migrations (column drops, NOT NULL additions on
existing data), test against a staging restore first.

#### Manage the suppression list

Site Settings → Email & Notifications → Suppression list. Address +
type (HardBounce / SpamComplaint / Unsubscribe / ManualSuppression) +
reason + created date. Manual remove warns about CAN-SPAM compliance.

#### Pastoral request to remove a member

The standard flow:

1. Confirm the request is from the member or an authorized representative.
2. Site Settings → Users → find the member → Hard delete (requires
   typing the display name to confirm).
3. The hard-delete cascades: profile photo deleted from Blob,
   `EmailBroadcastRecipient` rows have `UserId` nulled (snapshot fields
   preserved for audit), audit-log entries retained.

For data-removal requests under GDPR / CCPA scope: also manually
purge audit-log entries referencing the member's email and any
prayer-request submissions. Document the action in your operator log.

#### Urgent removal of a prayer request

Site Settings → Members & Community → Prayer Requests → find the
request → Delete. Soft-deleted; reappears on the admin Deleted tab.

For complete data erasure (rare, e.g., the request leaks PII), purge
the temporal history table directly via SQL:

```sql
DELETE FROM PrayerRequestsHistory WHERE Id = '<id>';
DELETE FROM PrayerRequests WHERE Id = '<id>';
```

#### Connect Card spam attack

Symptoms: `/admin/connect-cards` filling with low-quality submissions.

1. Configure Cloudflare Turnstile (Site Settings → Privacy & Security)
   if not already.
2. Tighten the rate limit: edit `Program.cs` `ConnectCardSubmit` policy
   from 5/hr to 1/hr.
3. Add common spam phrases to the profanity wordlist (Site Settings →
   Members & Community → ProfanityWordlist) — they'll bounce
   submissions automatically.

#### Rebuild the search index

```sh
# Deletes all SearchIndexEntry rows; the SearchIndexBootstrapService
# rebuilds on the next API boot (or you can wait for the daily refresh).
sqlcmd -S <server> -d <db> -Q "TRUNCATE TABLE SearchIndex;"
# Restart App Service
```

#### Manually trigger YouTube sync

Site Settings → Integrations → YouTube → "Sync now" button. Bypasses
the hourly tick.

#### Add or remove an Administrator

`/admin/users` → find user → Edit → toggle Administrator role → Save.
No special procedure; the role-membership change takes effect on the
user's next sign-in.

#### Forgotten admin password

The seeded admin is excluded from the password-reset flow. Recovery
requires direct database access:

```sql
-- Generate a new password hash via the Identity user manager:
-- (run this in a development environment with a connected DbContext)
-- var hasher = new PasswordHasher<ApplicationUser>();
-- var hash = hasher.HashPassword(user, "NewPassword!");

UPDATE AspNetUsers
SET PasswordHash = '<computed hash>',
    SecurityStamp = NEWID()
WHERE Email = '<seeded-admin-email>';
```

Then sign in with the new password and rotate.

### Troubleshooting

#### Email isn't sending

Site Settings → Email → check `EmailEnabled=true`. Run "Send test email"
and read the response. Most common cause: `EmailEnabled` was left
false (the production-safe default).

#### YouTube sync hasn't run

App Insights logs for `YouTubeSyncService`. Common causes: API key
missing, channel id wrong (use the slug, not the URL), daily quota
exhausted (~10,000 units; hourly polls use ~75/day so this is rare).

#### Members can't log in

1. Site Settings → Email — confirm the password-reset email is
   reaching the user (check suppression list).
2. `/admin/users` — confirm the user is Active (not Inactive / Locked).
3. Check Application Insights for failed sign-in attempts.

#### Search isn't returning results

Restart the App Service so `SearchIndexBootstrapService` reseeds the
index. If the issue persists, run the index rebuild SQL above.

#### Calendar feeds aren't updating

Calendar feeds are output-cached for 5 minutes (via the feed
controller's `[OutputCache]` attribute). After a write, allow 5 minutes
or invalidate the `calendar` tag.

### Multi-tenancy

v1 is single-tenant. The migration path to multi-tenant SaaS is
documented in [MULTI_TENANCY.md](./MULTI_TENANCY.md).

### Contributing

- Branch off `main` for feature work. Phase development used long-lived
  feature branches (e.g., `claude/credo-cms-phase-1-06gIY`); v1.x
  development can use `feat/<topic>` short-lived branches.
- PR process: code review + green CI; rebase rather than merge for a
  linear history.
- Code style: enforced via `.editorconfig` and `.eslintrc`. Run
  `dotnet format` + `npm run lint --fix` before pushing.
- Tests live alongside code in the `Domain.Tests` / `Application.Tests`
  / `Infrastructure.Tests` / `Api.Tests` projects (backend) and
  `app/src/**/__tests__/` (SPA).

