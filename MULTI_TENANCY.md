# Multi-Tenancy Path for Credo CMS

Credo CMS today serves **one church per deployment**. The architecture, however, is
built so that a future phase can convert it to a multi-tenant SaaS without a ground-up
rewrite. This document captures what's already designed for that future and what's
deliberately deferred, with reasoning, so that future contributors can extend safely.

---

## What's Already Multi-Tenant-Friendly

### 1. `SiteSettings` is a row, not a static configuration file

All per-church configuration — name, branding colours, contact info, social links,
theming inputs — lives in a single `SiteSettings` row in SQL. Adding a `TenantId`
column and changing the row from a singleton to a per-tenant lookup is a localized
schema change, not a refactor of every controller.

### 2. Blob path conventions reserve a tenant segment

When the real blob upload flows are added (Phase 2 onwards), the convention will be:

```
images/{tenantId}/{entityType}/{entityId}/{filename}
```

In Phase 1 we use `images/_default/...` but the path scheme already includes a tenant
segment. Tools that compute or list blob URLs accept a `tenantId` parameter today and
will continue to work unchanged when real tenant IDs flow through.

### 3. Entity IDs are `Guid`

Across the domain, every entity's primary key is `Guid` (sequential or random,
non-disclosing). Cross-tenant ID collisions are impossible by construction, which is
a precondition for either a shared-DB-shared-schema or shared-DB-separate-tenant-row
multi-tenant model.

### 4. Audit log carries actor, not actor-and-tenant

`AuditLogEntry` records `UserId`. When tenants land, `UserId` will simply continue to
identify the actor — adding `TenantId` as an additional column for filtering and
isolation is straightforward. The shape of the audit log doesn't need to change.

### 5. `ICurrentUserService` is the single funnel for "who am I"

When tenancy lands, an `ITenantContext` interface gets added next to it (from the
same DI container, the same composition root, populated from the same
`IHttpContextAccessor`). All other code consumes one or both abstractions; nothing
reaches into `HttpContext` directly.

### 6. SignalR hub paths and groups are flat

`/hubs/notifications` is a single hub with method-level authorization. To partition
real-time notifications by tenant later, we add a tenant-scoped group join on
`OnConnectedAsync` — no protocol change.

### 7. Identity user IDs are `Guid`

A future migration can add `TenantId` to `ApplicationUser` without colliding with
existing primary keys. (Some SaaS designs prefer email-uniqueness *per tenant* rather
than globally; that change is a non-breaking schema migration plus an Identity
configuration adjustment, not a rewrite.)

### 8. `Application` layer is database-agnostic

Because the `Application` project doesn't reference `Infrastructure` or EF Core, a
future shift from "shared DB, tenant column" to "DB-per-tenant" can be made entirely
inside `Infrastructure` (e.g., by parameterizing the `DbContext` connection string by
the resolved tenant) without touching `Application` services. This is the single most
important architectural decision protecting our multi-tenant future.

---

## What's Deferred to the Multi-Tenant Phase

These are *not* implemented in Phase 1 and would be done in a coordinated future
release. None of them require breaking changes that we cannot support.

### 1. `TenantId` on every per-tenant entity

Every content entity (Pages, News, Sermons, Events, Members, Groups, etc.) needs a
`TenantId Guid` column with a non-clustered index. EF Core `HasQueryFilter` is the
standard mechanism to scope every query automatically.

### 2. Tenant resolution middleware

A piece of middleware running before authentication resolves the tenant from one of:

- Hostname (`firstchurch.credocms.com` → tenant `firstchurch`)
- A subpath prefix (`/t/{tenantSlug}/...`)
- An explicit header (administrative tooling)

The resolved tenant is set on `ITenantContext` and bound to the request scope.

### 3. Shared-DB vs. DB-per-tenant decision

Two viable models:

| Model | Pros | Cons |
|---|---|---|
| Shared DB, `TenantId` on every row | Simplest ops; one schema, one migration | Per-tenant performance isolation is harder; one big query filter system |
| DB-per-tenant | Strong isolation; per-tenant restore is trivial | Connection-string management; per-tenant migrations |

The decision is **deferred** until we have at least the second tenant onboarded. The
`Application` layer's database-agnosticism makes this a localized decision.

### 4. Per-tenant Identity scoping

ASP.NET Core Identity uses globally unique emails by default. For a SaaS where the
same email might legitimately exist on two tenants, we either (a) override the
`UserStore` to scope email uniqueness per-tenant, or (b) keep emails globally unique
and use invitation flows to join existing accounts to additional tenants. Option (b)
is simpler and is the likely landing place.

### 5. Per-tenant theming

The theming system already supports a per-tenant church theme via `SiteSettings`.
What's deferred is the *system-theme branding hook* (currently the system theme is
fixed; in SaaS we want each church's logo+name visible in the system-theme top bar
even though the rest of the system theme is invariant). This is a small additive
change, not a rework.

### 6. Per-tenant audit log retention and GDPR exports

Multi-tenant SaaS introduces tenant-level retention preferences and per-tenant data
export/deletion (DSAR). Audit log entries already carry `UserDisplayNameSnapshot`
which simplifies user-deletion flows; adding per-tenant export is additive.

### 7. Per-tenant rate limiting

Phase 1 rate-limit policies are per-IP. Multi-tenant deployments will add per-tenant
quotas (e.g., emails-sent-per-month), enforced as separate policies.

### 8. Tenant administration ("super-admin")

A super-admin role outside the per-tenant Administrator/Editor/Member scheme manages
tenants themselves: provisioning, suspending, billing. This is a separate identity
domain and is intentionally not part of Phase 1.

---

## Safe-to-Refactor-Incrementally vs. Coordinated Migration

| Change | Safe incrementally? | Why / why not |
|---|---|---|
| Adding `TenantId` columns to entities | ✅ Mostly | New nullable column + default to current default tenant for legacy rows + migration to backfill + tighten constraint. Per-entity. |
| Adding `ITenantContext` and middleware | ✅ Yes | Additive; existing single-tenant deployments use a default tenant value. |
| Adding query filters | ⚠️ Per-entity | Each query filter must be applied + tested per entity. EF query filters are easy to forget; require a cross-cutting test. |
| Switching to DB-per-tenant | ❌ Coordinated | Connection-string resolution + migrations need a synchronized release; not safe to do incrementally. |
| Per-tenant Identity scoping | ❌ Coordinated | Touches login, password reset, invitation, email uniqueness — a single coherent change. |
| Per-tenant theming hooks in system theme | ✅ Yes | Additive UI change. |
| Per-tenant blob paths | ✅ Mostly | New uploads use the new prefix; legacy uploads keep their existing paths. A backfill job moves legacy assets into per-tenant prefixes. |
| Per-tenant rate limits | ✅ Yes | Add new policies; old per-IP policies remain as fallback. |

**The single coordinated migration that cannot be incremental** is the one that
introduces tenant resolution + the default-tenant convention + the query-filter
sweep. Until that lands, "single-tenant deployment" remains a first-class shape;
after it lands, "single-tenant" simply becomes "exactly one row in `Tenants`".

---

## Summary

Credo CMS is built so that flipping the multi-tenancy switch is an additive,
mostly-mechanical exercise. The hard architectural decisions — `Application`-doesn't-
reference-`Infrastructure`, `Guid` primary keys, blob paths reserve a tenant segment,
audit log includes `UserDisplayNameSnapshot`, theming is data-driven — are already
in Phase 1.
