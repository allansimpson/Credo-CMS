# Versioning in Credo CMS

Credo CMS uses **SQL Server temporal tables** for versioning of content entities.
This document describes how versioning is configured, the supporting code patterns,
the semantics of the **restore** operation, and the constraints that operators and
developers must understand before changing the schema.

> **Phase 1 status.** The infrastructure described here is in place: the
> `IVersionedEntity` interface, the `SaveChangesInterceptor`, the `ICurrentUserService`,
> the System User seed, the nightly trim `BackgroundService`, and the convention helper
> for declaring temporal tables in `OnModelCreating`. **No entity is yet versioned**;
> Phase 2 begins applying these patterns to the first content types (Pages, News, etc.).

---

## 1. Why Temporal Tables

SQL Server's system-versioned temporal tables provide:

1. **Automatic history** — every `UPDATE` and `DELETE` writes the previous row to a
   shadow history table with `SysStartTime` / `SysEndTime` columns. No application code
   is required to maintain history.
2. **Native query support** — `FOR SYSTEM_TIME AS OF`, `BETWEEN`, `ALL` and EF Core's
   matching `TemporalAsOf`, `TemporalBetween`, `TemporalAll` LINQ operators allow
   point-in-time and range queries with no extra plumbing.
3. **Storage efficiency** — history rows live in a separate physical table optimised
   for append-only writes; no double-write logic in the application.

The downside — addressed below — is that schema changes that drop columns from a
temporal table also lose those columns from history.

---

## 2. Convention: `IVersionedEntity`

Versioned entities must implement this interface, defined in `CredoCms.Domain`:

```csharp
public interface IVersionedEntity
{
    Guid? ModifiedByUserId { get; set; }
    DateTimeOffset ModifiedAt { get; set; }
}
```

These two columns are **required** at the database level (non-nullable in SQL even
though `Guid?` carries a nullable static type — the `Guid?` form exists only so that
the interceptor can set the value before save without needing a sentinel). Any insert
or update that bypasses the interceptor will fail loudly at the database due to the
NOT NULL constraint.

`ModifiedAt` is `DateTimeOffset` (not `DateTime`) so that history is unambiguous across
timezones and DST transitions.

---

## 3. Convention: `ICurrentUserService`

Defined in `CredoCms.Application`:

```csharp
public interface ICurrentUserService
{
    Guid UserId { get; }      // SystemUserId for background jobs / unauthenticated writes
    string DisplayName { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
```

The `Infrastructure` implementation reads from `IHttpContextAccessor` for HTTP requests
and falls back to `SystemConstants.SystemUserId` when no HTTP context is present (i.e.,
when a `BackgroundService` is writing). The System User is seeded with a fixed,
well-known Guid (`SystemConstants.SystemUserId`) and an account that is not loginable
(`IsActive = false`, no password hash, `EmailConfirmed = true`).

This means **every** write to a versioned entity records *who* made the change, even
the YouTube auto-sync writes coming in Phase 3.

---

## 4. The `SaveChangesInterceptor`

Registered globally on the `DbContext` via `services.AddDbContext<...>(o => o.AddInterceptors(...))`.

Pseudocode:

```csharp
public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
{
    foreach (var entry in eventData.Context.ChangeTracker.Entries<IVersionedEntity>())
    {
        if (entry.State is EntityState.Added or EntityState.Modified)
        {
            entry.Entity.ModifiedByUserId = _currentUser.UserId;
            entry.Entity.ModifiedAt = DateTimeOffset.UtcNow;
        }
    }
    return base.SavingChangesAsync(...);
}
```

This means application code simply does `db.Pages.Update(page); await db.SaveChangesAsync();`
and the audit columns are populated automatically.

---

## 5. Declaring an Entity as Temporal

In `OnModelCreating`:

```csharp
modelBuilder.Entity<Page>().ToTable("Pages", t => t.IsTemporal(temp =>
{
    temp.HasPeriodStart("ValidFrom");
    temp.HasPeriodEnd("ValidTo");
    temp.UseHistoryTable("PagesHistory");
}));
```

A small helper in `Infrastructure/Persistence/TemporalConfiguration.cs` (added in Phase 2
when the first versioned entity arrives) wraps this so the call site reduces to
`modelBuilder.Entity<Page>().AsTemporal();` with the period and history-table names
following a stable convention.

**Excluded from versioning by explicit project decision:**

- **Users (`ApplicationUser`)** — privacy. Changes to user records are logged in the
  audit log instead; a user's *historical* personal data is not retained beyond what's
  in the audit log.
- **Leaders** — explicit Phase 2 project decision (Leaders are presented as a curated
  public list, not a historical record).
- **Join tables (many-to-many)** — link changes are captured in the audit log instead.
- **The audit log itself** — entries are append-only and immutable by definition.

---

## 6. Querying History

EF Core 10 exposes temporal operators directly:

```csharp
// Point-in-time
var pageAsOf = await db.Pages
    .TemporalAsOf(asOfUtc)
    .FirstOrDefaultAsync(p => p.Id == pageId);

// All historical versions of a page
var versions = await db.Pages
    .TemporalAll()
    .Where(p => p.Id == pageId)
    .OrderBy(p => EF.Property<DateTime>(p, "ValidFrom"))
    .ToListAsync();

// Range
var inRange = await db.Pages
    .TemporalBetween(fromUtc, toUtc)
    .Where(p => p.Id == pageId)
    .ToListAsync();
```

The admin version-history endpoints (Phase 2 onwards) use these operators behind a
unified pattern:

- `GET    /api/admin/{entityType}/{id}/history`                    — list versions
- `GET    /api/admin/{entityType}/{id}/history/{versionTimestamp}` — fetch one version
- `POST   /api/admin/{entityType}/{id}/history/{versionTimestamp}/restore` — restore

---

## 7. Restore Semantics

**Restore copies a historical version's scalar/text fields into the current row.**
This produces a *new* current row with new `ValidFrom`/`ValidTo` and a new history
entry; the historical row remains untouched.

Critically, **relationships and link tables are NOT deep-restored.** Examples:

- Restoring a Page from before its category was changed does *not* re-attach the old
  category — the page stays in its current category.
- Restoring a Sermon does *not* re-attach the old Series or Scripture references.
- Restoring an Event does *not* re-attach old recurrence exceptions or registration
  links.

This is a deliberate choice: deep-restoring relationships across many-to-many edges and
soft-deletable parents creates "ghost" references and surprising side effects. The UI
explicitly states the rule when the user previews a restore. If a user wants to
restore relationships too, they do so manually after the scalar restore — the audit
log + version history give them everything they need to reproduce the old state.

The `<VersionHistoryPanel>` SPA component (scaffolded in Phase 1, used from Phase 2
onwards) takes a `diffStrategy` prop:

- `"prosemirror"` — for rich-text fields, uses `prosemirror-changeset` to render a
  semantic diff that respects document structure.
- `"html"` — for plain-HTML fields without ProseMirror, falls back to text-aware HTML
  diffing.
- `"text"` — for plain text, uses `diff-match-patch`.

---

## 8. Retention

Each versioned entity has a count-based retention policy: keep the **N** most recent
versions. The default is **20**, configurable in Site Settings (range 5–50, hard-capped
server-side).

A nightly `VersioningTrimBackgroundService` runs at 03:00 UTC by default and trims any
entity that exceeds its retention count by deleting the oldest history rows in batches.

> **Phase 1:** the background service is registered but inert (no versioned entities
> exist yet). Phase 2 onwards triggers actual trimming.

---

## 9. **Destructive-migration warning**

> **Read this before altering any temporal-table column.**

SQL Server temporal tables propagate certain schema changes to the history table. In
particular:

- **Dropping a column** drops it from history too. Historical rows lose that data
  permanently.
- **Changing a column's type** (e.g., `nvarchar(200)` → `int`) requires a temporary
  un-versioning, and improper migrations can lose history.
- **Renaming a column** is a drop+add by default — same problem.

**Use the add-then-deprecate pattern** for any non-trivial schema evolution on a
versioned entity:

1. **Migration A:** add the new column alongside the old one. Backfill the new column
   from the old one. Deploy.
2. **Migration B (later release):** mark the old column obsolete in code, but keep it
   in the schema. Deploy.
3. **Migration C (much later, after enough history has aged out under retention):**
   only then drop the old column.

For Phase 1 there are no versioned entities yet, so this risk is theoretical, but the
discipline must be in place from the moment Phase 2 introduces the first versioned
content type.

---

## 10. Blob URLs and Image Versioning

Versioned entities that reference blob-stored assets (logos, page images, sermon
artwork, etc.) must follow the **blob-URL-on-replace** pairing rule:

- The entity stores the **URL** of the asset, not the asset bytes.
- When a user replaces an image via the admin UI, the old blob is **kept, not
  overwritten**, and a new blob is uploaded with a fresh URL. The entity's URL field
  is updated to point to the new blob.
- Because the entity is versioned, the old version row retains the *old* URL and
  therefore can still display the *old* image when a previous version is viewed or
  restored.
- Blob lifecycle management (eventual cleanup of unreferenced blobs) is handled by a
  background scan pairing each blob with its most-recent referencing version-or-current
  row. Blobs whose every referencing row has aged past retention are eligible for
  deletion; that scan is added in Phase 5.

In Phase 1 the only image is the church logo (a single field on `SiteSettings`),
stored as a URL string. Phase 2 introduces the first real image-upload flows and the
blob-pairing rule begins to apply.

---

## 11. Code Pointers

| What | Where |
|---|---|
| `IVersionedEntity` | `CredoCms.Domain/Common/IVersionedEntity.cs` |
| `ICurrentUserService` | `CredoCms.Application/Common/ICurrentUserService.cs` |
| `CurrentUserService` impl | `CredoCms.Infrastructure/Common/CurrentUserService.cs` |
| `VersioningInterceptor` | `CredoCms.Infrastructure/Persistence/Interceptors/VersioningInterceptor.cs` |
| `VersioningTrimBackgroundService` | `CredoCms.Infrastructure/BackgroundServices/VersioningTrimBackgroundService.cs` |
| `SystemConstants` (Guids) | `CredoCms.Domain/Common/SystemConstants.cs` |
| `<VersionHistoryPanel>` shell | `app/src/components/shared/VersionHistoryPanel.tsx` |
