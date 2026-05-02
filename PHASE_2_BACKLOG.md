# Phase 2 Backlog

Tasks queued for Phase 2 that fall outside the original Phase 1 prompt but
were identified during Phase 1 implementation. These are still inside the
v1.0 scope (Phases 1–6); items deferred past v1 belong in `ROADMAP.md`.

---

## Repo Layout

### Rename `spa/` → `app/`

The frontend root folder is currently `spa/`. Phase 2 should rename it to
`app/` to match the more common convention (and because "SPA" is an
implementation detail — the folder may eventually host SSR pages, MDX
content packs, or a native shell).

This rename touches more than just the directory name. Every reference
listed below must be updated in the same commit so CI, dev tooling, and
deployments don't break:

**Filesystem**
- `spa/` → `app/` (the directory itself).

**Top-level docs** (paths and "the SPA" prose where appropriate)
- `README.md` — quick-start commands, project layout diagram.
- `BUILD_PLAN.md` — Stage G/H/I narrative references.
- `IMPLEMENTATION_NOTES.md` — every `spa/...` path mention.
- `VERSIONING.md`, `MULTI_TENANCY.md` — any code-path references.

**Build & deploy**
- `.github/workflows/deploy.yml` — `working-directory`, the
  `npm ci` / `npm run build` step paths, and the `cp -r spa/dist/* …`
  step that copies the build into `wwwroot/`.
- Any future workflow files (test, preview, lint).

**API integration points**
- `api/CredoCms.Api/Properties/launchSettings.json` — if the dev SPA
  proxy URL is referenced.
- Vite proxy / CORS allow-list configuration in `app/vite.config.ts`
  (currently `spa/vite.config.ts`) — paths referencing the API.
- Any `appsettings.Development.json` SPA-origin entries.

**Tooling configs inside the renamed directory**
- `package.json` `name` field (currently `"credo-cms-spa"` →
  `"credo-cms-app"`).
- `tsconfig*.json` `references` and `paths` entries that escape the
  directory (none today, but verify).
- Path aliases in `vite.config.ts` (`@` → `./src` is fine; just confirm
  no absolute path leaks).

**Editor / IDE**
- `.vscode/settings.json` if it pins workspace folders.
- Any `launch.json` / `tasks.json` `cwd` entries.

**Tests**
- Update any test files that reference `spa/` in fixtures or snapshots.
- Verify Playwright / Vitest config paths if they reference the folder
  by name.

**Acceptance criteria**
- `npm run build` and `npm test` work from `app/`.
- `dotnet test` still passes (no API tests reference the SPA path).
- The deploy workflow builds successfully end-to-end on a feature branch.
- `git grep -i "spa/"` returns only intentional matches (e.g., the
  acronym "SPA" in prose, never a path).
- No broken links in any markdown file.

**Suggested approach**
1. `git mv spa app` (preserves history per file).
2. Search-and-replace `spa/` → `app/` across all tracked files
   (`git grep -l "spa/" | xargs sed -i 's|spa/|app/|g'`, then review
   diff carefully — the acronym "SPA" in prose must NOT be touched).
3. Update the `package.json` `name` field manually.
4. Run `dotnet build`, `dotnet test`, `npm install`, `npm run build`,
   `npm test` to verify.
5. Push branch, confirm GitHub Actions `deploy.yml` succeeds end-to-end
   before merging.
