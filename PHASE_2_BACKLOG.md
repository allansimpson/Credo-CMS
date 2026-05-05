# Phase 2 Backlog

Tasks queued for Phase 2 that fall outside the original Phase 1 prompt but
were identified during Phase 1 implementation. These are still inside the
v1.0 scope (Phases 1–6); items deferred past v1 belong in `ROADMAP.md`.

---

## Repo Layout

### Rename `spa/` → `app/` ✅ Done

Completed in Stage P0 (commit `371b1cd`); leftover empty `spa/` directory
removed in Stage P1 (commit `4d21211`). Kept here for historical reference.

The frontend root folder was renamed from `spa/` to `app/` to match the
more common convention. References swept across docs, CI workflow,
package metadata, and tooling configs.

---

## Database Schema

### Drop the `AspNet` prefix from Identity tables ✅ Done

Completed in Phase 2 (commit on `claude/credo-cms-phase-1-06gIY`).
Migration `RenameIdentityTables` uses `RenameTable` operations
(data-preserving) plus FK + PK renames. All 7 Identity tables now use
the un-prefixed names: `Users`, `Roles`, `UserRoles`, `UserClaims`,
`UserLogins`, `UserTokens`, `RoleClaims`. Original entry kept below
for historical reference.

ASP.NET Core Identity defaults to prefixing every table with `AspNet`
(`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`,
`AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`). The prefix
exists for historical compatibility with classic ASP.NET Identity and
adds nothing in a greenfield app. Drop it so our schema reads cleanly
alongside the domain tables (`SiteSettings`, `AuditLogEntries`, and the
Phase 2+ entities to come).

**Final names**

| Before | After |
|---|---|
| `AspNetUsers` | `Users` |
| `AspNetRoles` | `Roles` |
| `AspNetUserRoles` | `UserRoles` |
| `AspNetUserClaims` | `UserClaims` |
| `AspNetUserLogins` | `UserLogins` |
| `AspNetUserTokens` | `UserTokens` |
| `AspNetRoleClaims` | `RoleClaims` |

**Implementation outline**

1. In `ApplicationDbContext.OnModelCreating`, after `base.OnModelCreating`,
   add `ToTable` overrides for each Identity entity:

   ```csharp
   modelBuilder.Entity<ApplicationUser>().ToTable("Users");
   modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
   modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
   modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
   modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
   modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
   modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
   ```

2. Generate the migration:

   ```bash
   dotnet ef migrations add RenameIdentityTables \
     -p CredoCms.Infrastructure -s CredoCms.Api -o Persistence/Migrations
   ```

3. EF should emit `RenameTable` operations (preserving data and FKs).
   Verify the generated migration uses `RenameTable` rather than
   `DropTable` + `CreateTable` — if the latter, the model isn't lined up
   correctly and existing rows would be lost.

4. Foreign-key constraint names also embed the old table names. EF will
   typically rename them automatically; if not, add explicit
   `RenameForeignKey` calls in the migration.

**Acceptance criteria**

- Migration applies cleanly to an existing Phase 1 database without data
  loss (test on a populated dev DB: seeded admin user still logs in,
  audit-log entries still resolve `UserId` to the right user).
- Migration's `Down()` restores the `AspNet*` names so a rollback works.
- All tests still green: `dotnet test`.
- A fresh `dotnet ef database update` against an empty DB produces only
  the un-prefixed names (no leftover `AspNet*` artifacts).
- README / IMPLEMENTATION_NOTES updated where the table names appear.

**Risks**

- Any raw SQL elsewhere that references `AspNetUsers` etc. would break.
  Audit `git grep -i "AspNet"` before merging — Phase 1 has no raw SQL
  hits, but Phase 2 search-infrastructure work (`P9.2 AddSearchIndexFullText`)
  will use raw SQL for `CREATE FULLTEXT INDEX`; if that lands first, it
  must reference the renamed table.

---
