# Credo CMS — Deferred Roadmap

Features explicitly **not** in the v1 (Phases 1–6) plan, queued for v1.x or later.
This list is taken directly from the Phase 1 prompt and is the authoritative
"this is intentionally deferred" reference.

---

## Member-Facing & Content

- **File upload and repeating field groups in Event Registration**
  Beyond the simple form fields shipped in Phase 3, registration may need file
  uploads (e.g., signed waivers) and repeating sub-forms (one entry per child
  in a family camp registration).
- **Generic form builder beyond event registration**
  A drag-drop form builder usable for any custom data-collection workflow.
- **Photo galleries**
  Photo grids and album-style galleries with lightbox viewing.
- **Sermon series → blog cross-linking**
  Curated linkage between blog posts and sermon series so that a series page can
  surface relevant posts and vice versa.
- **Class series → sermon cross-linking**
  Similar to above, between Class series and Sermons.
- **Sacramental milestones**
  Baptism, confirmation, marriage, and similar lifecycle records on member
  profiles, including private vs. public visibility rules.
- **Family / Household relationships**
  First-class household entities linking adults and children, with shared address,
  household-level communications, and per-member visibility into household data.
- **Member-side giving history integration**
  Currently, giving is external (e.g., link to Tithe.ly). A future phase wires a
  read-only view of the member's own giving history into their Credo profile.

## Communications

- **SMS via Twilio**
  An interface stub is added in Phase 5 alongside SendGrid; the real Twilio
  implementation comes later.
- **Live chat widget**
  Real-time visitor chat on public pages, integrated with member-area presence.
- **Multi-language / i18n**
  Per-language content variants, locale-aware date/time formatting,
  RTL support.

## Volunteers & Scheduling

- **Volunteer substitute requests**
  Self-serve "I can't make my shift; ask others" flow with notifications and
  swap confirmation.
- **Skill / qualification tracking on volunteers**
  Per-volunteer skill tags so that a Sound Tech assignment only suggests trained
  volunteers.
- **Automated volunteer scheduling**
  Constraint-based suggestion: given roles, dates, blackouts, skills, and
  availability, propose a schedule.

## Events & Calendar

- **Bulk recurrence exceptions**
  "Cancel every Wednesday in July" as a single operation rather than
  per-occurrence overrides.
- **"Move to different date" recurrence override**
  Beyond cancel-this-occurrence, allow moving a single occurrence of a recurring
  event to a different date+time without breaking the series.

## Multi-Campus / Multi-Tenant

- **Multi-campus support**
  Multiple campuses under one church (single tenant), each with independent
  service times, leaders, events, and a campus selector on the public site.
- **Per-tenant Identity scoping, super-admin, and DB-per-tenant model decisions**
  See `MULTI_TENANCY.md` for the planned multi-tenant migration; the items there
  are themselves the deferred work.

## Mobile & Apps

- **Native mobile app**
  iOS / Android wrappers around the SPA initially, native shell longer term.

## Performance & Infrastructure

- **Real-time output cache invalidation across entity types via Redis**
  Phase 2 introduces in-memory output caching for public read endpoints.
  Multi-instance deployments need Redis-backed cache invalidation broadcast
  across instances.

## Phase 5 deferred items (Communications)

- **SMS via Twilio** — interface + structural placeholder ship in v1
  (`TwilioSmsService` constructor throws). v1.5 implements + wires DI.
- **Volunteer substitute requests** — v1 ships lightweight signup /
  cancel; future iteration adds "request a substitute" flow.
- **Volunteer skill tracking + availability** — first-class skill tags
  on volunteers, availability windows, drag-and-drop scheduling.
- **Automated volunteer scheduling** — algorithmic match of skills +
  availability to upcoming roles.
- **Bulk recipient targeting beyond Groups** — segments like
  "members who haven't attended in N months" feeding the broadcast
  composer's target picker.
- **Branded newsletter-style templates** — visually-designed HTML
  templates with hero, multi-column layouts, click tracking. v1's
  template renderer ships simple inline-styled HTML.
- **Broadcast composer RTE** — current composer is textarea-based for
  v1; TipTap-based RTE with merge-field picker is a follow-up.
- **Per-recipient List-Unsubscribe-URL header** — v1 ships RFC 2369
  via broadcast-level mailto + body-footer per-recipient link;
  per-recipient HTTPS header via SendGrid Personalization.Headers is
  a follow-up.
- **Existing transactional caller refactor to use IEmailTemplateRenderer**
  — invitation / password reset / connect-card ack / group-join
  decision currently use inline strings; templates seeded but cutover
  deferred.

---

If a feature appears here that you (the church operator) urgently need, it can be
prioritized into a specific milestone via the issue tracker. The list is not
ordered by priority.
