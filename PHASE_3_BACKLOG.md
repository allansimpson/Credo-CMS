# Phase 3 Backlog

Tasks queued for the rest of v1.0 (Phases 4–6) that fall outside the
original Phase 3 prompt but were identified during Phase 3
implementation. Items deferred past v1 belong in `ROADMAP.md`.

---

## Security & Secrets

### Cloudflare Turnstile for public registration

**Status:** Deferred to Phase 4 per BUILD_PLAN Q-2 #4.

The Phase 3 prompt expected Turnstile to "already be configured for the
Connect Card pattern in earlier seeds; reuse" — but Connect Card is a
Phase 4 feature, so Turnstile infrastructure does not yet exist.

Phase 3's public event registration form is protected by:
- Honeypot field (visually hidden, must be empty on submit).
- Time-to-submit ≥ 5 seconds (prevents bot script-fast submissions).
- Per-IP rate limit: 5 submissions per 10 minutes.

When Turnstile lands in Phase 4 alongside Connect Card, the
infrastructure to add:

1. Site Settings → Privacy & Security tab:
   - `TurnstileSiteKey`, `TurnstileSecretKey` (treated as secret per the
     same masking convention as YouTube secrets).
2. Reusable `<TurnstileWidget>` SPA component.
3. Server-side `ITurnstileVerifier` calling
   `https://challenges.cloudflare.com/turnstile/v0/siteverify`.
4. Apply to the public event registration endpoint, the Connect Card
   submit endpoint, and the public Prayer Request submit endpoint.

### Data-Protection encrypt-at-rest for YouTube secrets

**Status:** Phase 3 stores `YouTubeApiKey` and `YouTubeOAuthRefreshToken`
as plain text in the SiteSettings row, masked in the admin UI (last 4
chars visible; Reveal button never round-trips the full value to the
client unless the admin explicitly clicks it).

For v1.x, wrap the get/set with ASP.NET Core
`IDataProtector` (purpose `"credo-cms.site-settings.secrets"`) so the
DB column holds an encrypted blob instead of the raw secret. The schema
doesn't change; only the get/set boundary in
`SiteSettingsService.UpdateAsync` and `GetAdminAsync` (with a "reveal"
endpoint for the masked-by-default admin UI).

The data protection key ring should live outside the DB (file system
on-prem; Azure Blob Storage with Key Vault wrapping in production).

---

## Phase 3 in-scope items intentionally not done in P3

(none expected up front; this section will accumulate items that
surface during implementation)
