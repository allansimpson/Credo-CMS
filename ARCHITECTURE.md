# Architecture

This document is the authoritative "why" record for Credo CMS's
shape — the architectural decisions a future maintainer needs to
understand the codebase. Implementation notes for individual phases
live in [IMPLEMENTATION_NOTES.md](./IMPLEMENTATION_NOTES.md); this
document captures only the durable patterns.

## High-level

```
┌─────────────────┐   ┌──────────────────┐   ┌────────────────────┐
│  React SPA      │   │  Astro docs site │   │  ASP.NET Core API  │
│  (admin + pub)  │   │  (operator help) │   │  + SignalR + auth  │
└────────┬────────┘   └────────┬─────────┘   └─────────┬──────────┘
         │                     │                       │
         │  /api/* (JSON)      │  /docs/* (static)    │
         │  /hubs/*  (SignalR) │  via DocsController  │
         └─────────────────────┴───────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
       ┌──────▼─────┐  ┌──────▼──────┐ ┌──────▼──────┐
       │ Azure SQL  │  │ Azure Blob  │ │  SendGrid   │
       │ + temporal │  │ (images +   │ │  (or SMTP / │
       │  history   │  │ documents)  │ │  Logging)   │
       └────────────┘  └─────────────┘ └─────────────┘
```

## Solution layout

Four .NET projects (DDD layers) + an Astro site:

- **CredoCms.Domain** — entities, enums, domain interfaces. No
  framework dependencies beyond `System.ComponentModel.DataAnnotations`
  for shaped attributes.
- **CredoCms.Application** — services, DTOs, repository interfaces,
  validators. Depends on Domain only. Uses FluentValidation, no
  Entity Framework directly.
- **CredoCms.Infrastructure** — EF Core, repositories, third-party
  integrations (SendGrid, MailKit, YouTube, ImageSharp, ProfanityFilter).
  Implements Application interfaces; depends on Domain + Application.
- **CredoCms.Api** — ASP.NET Core host, controllers, SignalR hubs,
  middleware. Depends on Infrastructure. Composition root.

Test projects mirror this: `CredoCms.Domain.Tests`,
`CredoCms.Application.Tests`, `CredoCms.Infrastructure.Tests`,
`CredoCms.Api.Tests`. Apply tests at the layer they cover (no
integration test in `Domain.Tests`; no in-memory DB test in
`Application.Tests`).

## Authentication & authorization

ASP.NET Core Identity with cookie auth. Three roles:

| Role | Capabilities |
|---|---|
| Member | Public site + member-only content + own profile + groups + prayer + connect card |
| Editor | + admin shell (content management) |
| Administrator | + Site Settings + user management + email templates + audit log |

Roles are cumulative. Authorization policies in `Program.cs`:
`AnyAuthenticated`, `AdminShell` (Editor or Admin), `AdministratorOnly`.

Service-layer permission checks are the primary defense (not
controller attributes alone). Phase 4 added explicit
`if (UserId != currentUserId) throw ForbiddenException` guards on
every member-data endpoint.

## SignalR groups

`NotificationHub` exposes three groups:

- `admins` — Editors + Administrators auto-join on connect.
- `members` — Members + Editors + Administrators auto-join on
  connect (so admin shells receive the same stream as the public
  prayer wall).
- `user-{userId:N}` — auto-join per authenticated user; used for
  user-targeted notifications (group-membership decisions, prayer
  request resolutions, broadcast send progress).

The hub is in `CredoCms.Api`; the abstract notifier interface
`IRealtimeNotifier` lives in Application so services can emit events
without referencing SignalR directly.

## Versioning (temporal tables)

Versioned entities (Pages, News, Blog, Sermons, Documents, Leaders,
Events, ClassSlot/Offering, Group, PrayerRequest, EmailBroadcast,
EmailTemplate, EventVolunteerRole) use SQL Server temporal-table
history. Configuration helper `AsTemporal(historyTableName)` in
`CredoCms.Infrastructure.Persistence` keeps the conventions DRY.

A `VersioningInterceptor` populates `ModifiedAt` + `ModifiedByUserId`
on save. The interface marker `IVersionedEntity` is the contract.
`VersioningTrimBackgroundService` periodically prunes history beyond
the configured retention count.

See [VERSIONING.md](./VERSIONING.md) for the full lifecycle.

## Output cache

`Microsoft.AspNetCore.OutputCaching` with tag-based invalidation. Tags
declared as `OutputCacheTags.{Pages, News, Blog, Sermons, Events, ...}`
in Application. Controllers add `[OutputCache(Duration = N, Tags = ...)]`
attributes. Mutations call `IOutputCacheInvalidator.InvalidateAsync(tag)`
which wraps `IOutputCacheStore` from ASP.NET Core.

Single-instance App Service is the v1 deployment target; multi-instance
scale-out needs Redis-backed cache invalidation broadcast across
instances (tracked in ROADMAP).

## Privacy contracts (Phase 4)

Two-layer privacy enforcement on member data:

1. **DB-level opt-in gate**: `ApplicationUser.IsListedInDirectory`
   filters who appears in the directory at all.
2. **Service-level field filter**: `MembersDirectoryService` strips
   non-opted-in fields (email/phone/address/photo) from each row
   before serialization.

Compile-time-distinct DTO shapes for sensitive surfaces: e.g.,
`PublicClassSlot` vs `MemberClassSlot` so anonymous + member callers
get different JSON keys at the wire level — JSON serialization simply
omits absent member-only properties for anonymous callers, no
runtime null-checks.

## Real-time push pattern

Every cross-cutting state change emits a SignalR event to a relevant
group:

- Content publish → `admins` group.
- Group join request → `admins` + group leader's `user-{userId:N}` channel.
- Group decision → requester's `user-{userId:N}` channel.
- Prayer request lifecycle → `members`.
- Connect card submit → `admins`.
- Broadcast send → `admins` (for live stats updates).
- Volunteer slot fill / open → `admins`.

The SignalR client is the same on public + admin SPAs; what differs is
which group the connection is in (driven by `OnConnectedAsync`).

## Email + suppression

`IEmailService` has three implementations selected per call by
`EmailServiceRouter` based on `SiteSettings.EmailProvider`:

- `LoggingEmailService` — Serilog-only (default; no outbound mail).
- `SendGridEmailService` — full-featured with chunked broadcast +
  per-recipient personalizations.
- `SmtpEmailService` — MailKit-backed; one connect per send.

Suppression list (`EmailSuppression` entity) tracks hard-bounce + spam-
report + unsubscribe addresses. Transactional sends BYPASS suppression
per CAN-SPAM exemption; broadcast sends respect it via bulk lookup.

The SendGrid webhook (`/api/webhooks/sendgrid`) verifies via the SDK's
`RequestValidator` ECDSA helper, dedupes events by `sg_event_id`, and
updates suppression + recipient + broadcast stats in one pass.

## Background services

| Service | Tick | Purpose |
|---|---|---|
| `BroadcastSendWorker` | 15s | Picks up Sending broadcasts + due Scheduled broadcasts; dispatches via `IEmailService`. |
| `ScheduledPublishingService` | 60s | Publishes News + Blog with `ScheduledPublishAt <= now`. |
| `AdminNotificationDigestService` | 5min | Sends digest emails to Editors + Admins per cadence. |
| `EventVolunteerReminderService` | 1hr | 24-48h reminders for volunteer signups. |
| `YouTubeSyncService` | 1hr | Auto-imports new videos as draft Sermons. |
| `SearchIndexBootstrapService` | startup | Backfills the search index on first boot. |
| `VersioningTrimBackgroundService` | nightly | Prunes temporal history beyond retention. |

## Multi-tenancy posture

v1 is single-tenant. Every entity references the implicit single Site
Settings row at id `SystemConstants.SiteSettingsId`. The migration
path to multi-tenant SaaS is documented in [MULTI_TENANCY.md](./MULTI_TENANCY.md):
add a `TenantId` column to every entity, scope queries via a global
filter on `IApplicationDbContext`, swap the Site Settings singleton
for a per-tenant lookup.

## Observability

Serilog → console (dev) + Application Insights (prod). Audit log
(`AuditLogEntry`) captures cross-cutting mutations with user display
name + IP at the time. SignalR connections logged at debug; HTTP
requests at information.

## Phase boundaries

The codebase grew through six explicit phases (1-6). Each phase
shipped an end-to-end vertical slice:

1. Foundation: layered solution, identity, theming, admin shell, audit log, Site Settings, SignalR, Bicep templates.
2. Core content: Pages, News, Service Times, Leaders, Documents, search, banner, system pages, SEO, output caching, image upload + WebP.
3. Sermons + Events: YouTube sync, transcripts, Scripture references, recurrence, registration, iCal feeds.
4. Members + Community: full member profiles + directory, Groups, Classes, Prayer Requests, Connect Card, Blog, Facebook OAuth.
5. Communications: real email delivery (SendGrid/SMTP/Logging), broadcast composer, scheduled publishing, email-on-publish, templates, webhooks, notification batching, one-click unsubscribe, SMS stub, volunteer signups.
6. Polish: Astro docs site, GA4 + cookie consent, RSS feeds, accessibility audit, mobile audit, SEO refinement, README expansion, deployment dry-run docs.

Each phase's deliverables are intact in subsequent phases — Phase 6
does not rebuild Phase 1's auth or Phase 5's broadcast worker; it
adds + refines.
