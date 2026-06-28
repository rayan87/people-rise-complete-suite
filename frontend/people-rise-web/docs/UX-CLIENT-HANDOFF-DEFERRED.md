# UX — Client Handoff (DEFERRED to after Phase 1)

**Status: DO NOT IMPLEMENT YET.** This document captures the work required to take the frontend from
**Model A** (People Rise's internal consultant tool) to **Model B** (the client organization's own HR
staff log in). We are currently in Phase 1, calibrating the methodology/scoring engine at El-Delta, and
the app is intentionally still a consultant tool. Pick this up only once Phase 1 is signed off.

The central point: **the app currently has no role awareness.** The data is already there —
`TenantAccess.role` exists (`src/app/core/models.ts`) and tenancy is access-controlled server-side — but
nothing in the UI reads it. Every screen and button is shown to everyone. The first task below is the
spine the rest depend on.

## 0. The role model (do this first)

- Add a `role` signal to `Session` derived from `activeTenant().role`, plus helpers like
  `isConsultant()`, `isHrOrAbove()`, `canEdit()`. Define the actual role enum/matrix with the product
  owner before coding.
- Add **route guards** in `app.routes.ts`:
  - consultant-only: methodology list + `version-editor` (authoring is the consulting IP),
    grading structure config, the demo seed.
  - HR-and-above-only: Salary Builder and any future compa-ratio / sensitive views (enforced in CLAUDE.md
    compensation rules — "never expose to the employee").
- Conditionally render affordances by role: a viewer should see evaluations but not New / Approve /
  Edit / Delete.

Everything below assumes this layer exists.

## 1. Hide / lock consultant-only surfaces for client roles

- **Demo seed button** (`settings.ts`) — a client seeing "Create El-Delta demo client" is an obvious
  internal-tooling leak. Hide for non-consultants.
- **Cross-client tenant switcher** (`app.html` topbar `<select>`). Access control already limits the
  list to granted orgs and auto-selects when there is one (`session.ts loadTenants`). When the user has
  a single tenant, collapse the picker to a **static org-name label** — no dropdown, no
  "— select client —".
- **Methodology editor** — read-only or hidden for client roles. Clients consume evaluations against a
  published version; they do not edit the scoring engine.
- **Salary Builder / band views** (`salary.ts`, the band bar in `job-detail.ts`) — gate behind HR+.

## 2. Professional perception / no dev leaks

- **Sanitize error messages.** `app.ts` surfaces the dev URL verbatim ("Could not reach the API on
  `http://localhost:5080`") and tells the user to check `X-User-Id`; every mutation also dumps raw
  backend `e.error.detail`. Clients must see plain-language messages — never a header name, a localhost
  address, or a stack-trace-ish detail. Add a mapping layer (and move `API_BASE` to real config).
- **Real identity in the sidebar.** "Dev User / Consultant" is hardcoded in `app.html` (≈ lines 72–73) —
  bind to the authenticated user and their actual role once real auth exists.
- **Branding / white-label decision.** It is "People Rise" everywhere. Decide with the product owner
  whether Model B is white-labeled (client logo/name in the chrome) or co-branded, before more chrome is
  built. Affects the brand block in `app.html` and the favicon/title.
- **Consultant-framed copy pass.** Strings like "Select a client above first" and "Overview of this
  client's job & reward design" address the user *about* the client — wrong when the user *is* the
  client. Sweep `i18n.ts` for second-person-about-the-client phrasing.

## 3. Accountability becomes a feature

- Surface **who approved and when**. `approvedAt`/`submittedAt` exist on the evaluation DTO but are not
  displayed; there is no actor shown. A client's comp committee will ask "who signed off on this grade."
  Show approver + timestamp on the evaluation detail, and consider an activity/audit panel built on the
  existing immutable answer trail.

## 4. Guardrails for non-expert users

(If the Phase-1 confirm dialog from `UX-PHASE1-INSTRUCTIONS.md` A2 is in place, reuse it.)
- Confirm + consequence text on every irreversible/destructive action: delete, regenerate bands,
  publish version. A consultant knows the blast radius; a client HR generalist does not.

## 5. First-run / onboarding

- A consultant lands on an empty tenant and knows to seed. A client landing on a blank dashboard with one
  small info alert is lost. Build a genuine empty-state / onboarding path for clients (guided "what to do
  first"), and ensure it is **not** the demo-seed button.

## 6. Broader-audience hardening

- **Mobile/responsive shell.** The sidebar is a fixed 220px with no collapse (`app.css`); only the
  dashboard has a media query. A wider, possibly mobile client audience needs a collapsible/hamburger
  sidebar and responsive tables.
- **Full accessibility pass** beyond the Phase-1 quick wins: keyboard navigation across all flows,
  screen-reader labels everywhere, focus management in dialogs, color-contrast audit in both themes,
  reduced-motion. With a non-technical audience these become adoption blockers, not polish.

## 7. Tenancy / client management (Model A operations)

Today, client orgs (tenants) only come into existence via the **demo seed** button; there is no UI to
create, list, configure, or archive real clients. A consultant operating Model A at scale will need:
- a **Clients** admin screen (list tenants the user can access, their status/owner type, last activity),
- a real **provision-new-client** flow (this touches DB-per-tenant provisioning in the API — heavier than
  a normal form; coordinate with backend),
- pairs naturally with the prominent workspace switcher (C5 in the Phase-1 doc) and the role model (§0).

**Explicitly out of scope for the Phase-1 calibration gate.** Calibration is single-tenant accuracy work
inside El-Delta (the seed already provides that client); general client management does not advance it.
Deferred here on purpose.

## 8. Per-user preferences

- Language currently persists per-browser in `localStorage`. For client orgs, consider per-user
  server-side preferences (default language — many Egyptian HR users will want Arabic by default) so the
  experience follows the user across devices.
