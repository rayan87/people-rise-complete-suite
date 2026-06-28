# UX Fixes — Phase 1 (consultant tool, calibration stage)

**Audience:** implementer (Sonnet). **Scope:** the Angular app in `frontend/people-rise-web` only.
**Context:** We are in **Model A** — People Rise is the consultant's internal tool. We are currently
**calibrating the methodology and scoring engine against real El-Delta jobs**. Prioritize anything that
makes authoring/tuning a methodology and reading evaluation results faster and less error-prone.

Do **not** touch the client-handoff concerns (role gating, branding, error sanitization, onboarding) —
those are deliberately deferred to `UX-CLIENT-HANDOFF-DEFERRED.md` and implemented after Phase 1.

## Ground rules (do not violate)

- **UI only. Never change how scores are computed.** Scoring is server-side and authoritative
  (LOCKED RULE 9). These tasks must not alter totals, factor subtotals, or grade resolution.
- Match the existing style: standalone components, signals, `@if/@for` control flow, file-scoped
  template strings, design tokens from `src/styles.css` (no hardcoded colors), logical CSS properties
  (`inset-inline`, `margin-inline`) so RTL keeps working.
- All new user-facing strings go through `I18n` (`i18n.t(...)`) with **both** `en` and `ar` entries in
  `src/app/core/i18n.ts`. No bare English in templates.
- Run `npm run build` (or `ng build`) after changes; fix any warnings you introduce.

---

## A. Cross-cutting infrastructure (build these first — later tasks depend on them)

### A1 — Toast / notification service  `[priority: high]`
**Problem:** Mutations report success/failure only through scattered per-component `error()`/`notice()`
signals, and several actions (job create, job edit, salary save) give **no success feedback at all** —
the form just closes. During calibration you run many saves and need to know each one took.

**Do:**
- Add `src/app/core/toast.ts` — a `providedIn: 'root'` service holding a signal array of
  `{ id, kind: 'ok'|'error'|'info', text }`, with `success(text)`, `error(text)`, `info(text)`, and
  auto-dismiss (~4s) plus manual dismiss.
- Render a fixed-position toast stack in `src/app/app.html` (top-right in LTR, top-inline-end so RTL
  mirrors it). Style with existing alert tokens (`--success-soft`, `--danger-soft`, `--primary-soft`).
- Route the existing success/failure paths through it: `version-editor` publish/add/save,
  `salary` generate/save, `jobs` create/edit/delete, `evaluation-detail` submit/approve,
  `settings` seed, `methodology-detail` save/new-version.
- Keep inline `error()` alerts for **blocking** load failures (e.g. "Failed to load evaluation");
  use toasts for the **outcome of an action**.

**Acceptance:** every create/update/delete/publish/approve/generate shows a toast on success and on
failure. No silent successes remain.

### A2 — Styled, bilingual confirm dialog  `[priority: high]`
**Problem:** Destructive confirmation uses the native `confirm()` (`jobs.ts` ~line 379) — unstyled,
English-only (breaks Arabic), and inconsistent with the app.

**Do:**
- Add a small confirm dialog (service + component, or a reusable modal component) that returns a
  `Promise<boolean>`, with title, body, confirm label, cancel label, and a `danger` variant (red
  confirm button). RTL-aware, dismissible on backdrop click and `Esc`, focus trapped, confirm button
  autofocused.
- Replace the `confirm()` call in `jobs.ts deleteJob` with it.
- This component is reused by C3 and C4 below.

**Acceptance:** deleting a job opens the styled dialog; it works in Arabic/RTL; keyboard `Esc` cancels.

---

## B. Calibration-supporting fixes (highest value right now)

### B1 — Fix question/option ordering bug in the methodology editor  `[priority: high]`
**Problem:** In `src/app/features/evaluation/version-editor.ts`, `addQuestion()` and `addOption()`
**hardcode `sortOrder: 1`** (≈ lines 188, 192). Every question in a factor and every option in a
question is created with the same sort order, so display order is undefined and ties collide. While
calibrating you reorder questions/options constantly — this makes order unreliable.

**Do:**
- When adding a question, compute `sortOrder = (max sortOrder of f.questions) + 1` (or
  `f.questions.length + 1` if list is contiguous). Same pattern for options using `q.options`.
- Leave the existing **edit** flow (which already lets you set `sortOrder`) as is.

**Acceptance:** newly added questions/options append in order; no two siblings share a `sortOrder`.

### B2 — Grade-mapping gap & overlap validation  `[priority: high]`
**Problem:** In `version-editor.ts`, grade mappings (`[minScore, maxScore]` → grade) have no validation.
A **gap** means a job whose total lands in the gap resolves to **no grade** (shows `—`); an **overlap**
means ambiguous grading. Both directly corrupt calibration results, silently. (Note: this is read-only
analysis in the UI — the engine still resolves authoritatively server-side; we are only surfacing
problems to the consultant.)

**Do:**
- In the grade-mappings card, add a computed validation that sorts the version's `gradeMappings` by
  `minScore` and reports, as inline warnings (use `.alert info`/a warn style, not blocking):
  - **overlaps:** any pair where ranges intersect — name the two grades.
  - **gaps:** any uncovered integer range between consecutive mappings — show the missing `[a, b]`.
  - **inverted ranges:** `minScore > maxScore`.
- Purely client-side from already-loaded data. Do not block publish on it (warn only) unless trivial.

**Acceptance:** with El-Delta mappings that have a deliberate gap or overlap, the editor shows a clear,
specific warning naming the grades/ranges involved.

### B3 — Calibration view: evaluations ranked by score  `[priority: medium]`
**Why:** The calibration gate (see root `CLAUDE.md`) is "score 6–10 known El-Delta jobs and check they
rank in expected seniority order." The evaluation hub currently lists evaluations in load order with no
ranking.

**Do:**
- In `src/app/features/evaluation/evaluation-hub.ts`, add a sort control (at minimum: sort by
  `totalScore` descending) and a **rank column** (1, 2, 3…) shown when sorted by score. Only rank
  scored rows (Submitted/Approved); leave Draft rows unranked.
- Keep it simple — client-side sort of the existing `evaluations()` array. No new endpoint.

**Acceptance:** the consultant can open Job Evaluation, sort by score, and read off rank order to compare
against expected El-Delta seniority.

---

## C. Navigation & safety bugs

### C1 — Information architecture: give Grading Structure a real home + fix nav  `[priority: high]`
**Problem (root cause):** The product's headline is the trio **Job Evaluation + Grading Structure +
Salary Builder**, but "Grading Structure" has no nav home, and the word "structure" is overloaded across
three unrelated things:
- The dashboard module card links to `/grading` (`dashboard.ts` ~line 267), which has **no route** —
  the `**` wildcard silently redirects to the dashboard (dead link).
- `/settings/structure` ("Structure", under *Configuration*) is actually **Levels / Job Families /
  Grades reference-data CRUD** (`settings-structure.ts`) — taxonomy setup, *not* the grading deliverable.
- The real **level × family grade grid** (the Grading Structure deliverable per root `CLAUDE.md`) is
  buried as the "Structure" toggle **inside the Jobs page** (`jobs.ts`, `view() === 'matrix'`).

This is a four-part IA fix. Treat it as one coherent change.

**C1a — Extract the grade grid into a dedicated screen (`/grading`).**
- Create `src/app/features/grading/grading.ts` (selector `pr-grading`, `<h1>` = "Grading Structure").
- **Move** the matrix from `jobs.ts` into it: the `matrixLevels`, `matrixFamilies`, `hasUnassigned`,
  `outOfScopeLevels` computeds and the `jobsAt(levelId, familyId)` method, the entire
  `@if (view() === 'matrix')` template block, and the `.matrix*` / `.job-card` / `.cell-empty` styles.
  It loads `jobs`, `levels`, `families` (same as `jobs.ts` does today) — it does **not** need grades or
  any new endpoint.
- **Remove** the matrix from `jobs.ts`: delete the `view` signal, the List/Structure view-toggle in the
  header, the `@if (view() === 'matrix')` block, and the now-unused matrix computeds/styles. Jobs becomes
  a clean **list-only** screen (keep its filters, create/edit/delete, and the list table).
- Add the route in `app.routes.ts`: `{ path: 'grading', loadComponent: () => import('./features/grading/grading').then(m => m.Grading) }`. This also makes the dashboard's existing `/grading` link valid.

**C1b — Rename the taxonomy screen.**
- `/settings/structure` stays where it is (it *is* configuration) but is relabeled from "Structure" to
  **"Levels, Families & Grades"** (or "Taxonomy") in the sidebar, the page `<h1>`, and `app.ts pageTitle()`.
  The route path can stay `/settings/structure`.

**C1c — Restructure the sidebar nav** (`app.html`) to match the workflow and product story:
```
Dashboard
─ Job & Reward Design ─          (pipeline order)
   Job Library
   Job Evaluation     ●badge
   Grading Structure   → /grading
   Salary Builder
─ Configuration ─
   Levels, Families & Grades   → /settings/structure
   Methodologies               → /methodology   (moved down from the main group)
   Settings                    → /settings
```
- Move **Methodologies** out of the main module group into Configuration (it is scoring *config*, not a
  deliverable).
- Remove the **"Workspace"** group label. The active-client context is consolidated into a single
  prominent switcher — see **C5**; do not leave both the sidebar row and the topbar select. Settings
  becomes a normal bottom item under Configuration (drop the "Workspace" heading).

**C1d — Fix the dashboard links.**
- Point the "Grading Structure" module card (`dashboard.ts` ~line 267) at `/grading` (now real).
- Optionally make the pipeline's "Grade structure" step link to `/grading` too.

**i18n:** all new/renamed labels go through `I18n` with `en` + `ar` (`nav.jobLibrary`, `nav.grading`,
`nav.structure` → "Levels, Families & Grades", `nav.methodology`, and the group headings). This overlaps
**C2** below — do them together and treat C2 as folded into this task.

**Acceptance:**
- "Grading Structure" appears as a top-level item in the **Job & Reward Design** group and opens the
  level × family grade grid; the dashboard card and pipeline step reach the same screen.
- Jobs is list-only with no leftover matrix code; the grid renders identically on `/grading` to how it
  did inside Jobs (same out-of-scope-level alert, unassigned column, grade chips).
- Methodologies and "Levels, Families & Grades" sit under Configuration; no "Workspace" group and no
  duplicate tenant row remain.
- Every sidebar item and the topbar title are translated in Arabic/RTL.

### C2 — Translate the remaining nav / page-title strings  `[priority: medium]`
> **Note:** if you do C1 (which restructures the nav and routes every label through i18n), this task is
> largely absorbed into it. Keep C2 as the checklist to confirm nothing was missed.

**Problem:** `app.ts pageTitle()` and `app.html` hardcode English for "Methodologies", "Structure",
"Job Library". In Arabic/RTL mode these stay English while the rest flips.

**Do:** move them into `i18n.ts` (`nav.methodology`, `nav.structure`, `nav.jobLibrary`, etc.) and use
`i18n.t(...)` in both the sidebar and the `pageTitle()` computed. Also reconcile "Jobs" vs "Job Library"
to one term.

**Acceptance:** switching to Arabic translates every sidebar item and the topbar title.

### C3 — Confirm before "Generate bands" overwrites  `[priority: medium]`
**Problem:** In `salary.ts`, `generate()` replaces all band rows, silently discarding any manual
per-grade edits. During calibration you may have hand-tuned a band and then regenerate.

**Do:** if any rows already have a `band`, open the A2 confirm dialog
("This replaces all existing bands, including manual edits. Continue?") before calling `generateBands`.

**Acceptance:** regenerating with existing bands prompts; with no bands it runs directly.

### C4 — Confirm before publishing a methodology version  `[priority: medium]`
**Problem:** Publishing is irreversible (a published version becomes read-only and is what evaluations
pin to — LOCKED RULE 8). `version-editor.ts publish()` fires with no confirmation.

**Do:** wrap `publish()` in the A2 confirm dialog explaining it makes the version permanent/read-only
and is the version future evaluations will use.

**Acceptance:** publish prompts; cancel aborts; confirm publishes and toasts success.

### C5 — Prominent active-client (workspace) switcher  `[priority: medium]`
**Problem:** The active client is the scope of every screen, but it lives in a thin `— select client —`
`<select>` in the **top-right** of the topbar (`app.html`) — the lowest-attention spot — and is
duplicated by a dead display row in the sidebar. The most important context is the least visible.

**Do:**
- Consolidate to **one** switcher, placed **top-left at the top of the sidebar**, merged with / directly
  under the brand block. The **client name is the prominent element** (not a faint pill); clicking it
  opens the switcher (a menu or the existing `<select>` restyled).
- **Remove the topbar `— select client —` select** and the non-link sidebar tenant row. One source of
  truth.
- Graceful collapse: when `session.tenants().length === 1`, render the client name as a **static label**
  (no dropdown affordance). When there are several (the consultant case), it's an interactive switcher.
- Keep the existing behavior: `onTenantChange` → `session.setTenant`, persisted to `localStorage`.
- (Optional) echo the active client name in the topbar page header so it's visible while scrolling.

**Acceptance:** the active client is clearly visible top-left on every screen; switching works; a
single-tenant user sees a clean label, not an empty-looking dropdown; no duplicate tenant UI remains.

> Note: this is the Model A (consultant) switcher. The `UX-CLIENT-HANDOFF-DEFERRED.md` role work later
> hides switching entirely for single-org client logins — the graceful-collapse behavior here is the
> first step toward that.

---

## D. Accessibility quick wins (cheap, do alongside the above)

### D1 — Focus visibility  `[priority: medium]`
`src/styles.css` sets input focus to `outline: 2px solid var(--primary-soft)` (a pale tint — barely
visible). Change the focus ring to `var(--primary)` (keep the soft tint only as an optional surrounding
glow). Verify in both themes.

### D2 — Labels on icon-only buttons  `[priority: medium]`
The edit/delete icon buttons in `jobs.ts` (≈ lines 169–172) have no accessible name. Add
`[attr.aria-label]` (translated) and `title` to each. Audit other icon-only buttons for the same.

### D3 — Keyboard-reachable row navigation  `[priority: low]`
Dashboard puts `routerLink` on `<tr class="clickable">` (`dashboard.ts` ~line 232) — not focusable and
not real link semantics. Either make the job title cell a real `<a routerLink>` (preferred) or add
`tabindex="0"` + keydown(Enter) handling to the row.

### D4 — Anchor-as-button back links  `[priority: low]`
`new-evaluation.ts` (~line 15) uses `<a (click)="router.navigate(...)">` with no `href` — not keyboard
operable. Replace with `routerLink="/evaluation"` to match the pattern used in the other detail screens.

---

## E. Readability & density (type scale)

### E1 — Raise the type scale and loosen density  `[priority: high]`
**Problem:** The UI reads too small — users are zooming the browser to ~130% to work comfortably. `body`
sets **no base font-size**, then components override *downward*: a large amount of secondary text is at
**10–11px** (`.brand-sub`, `.nav-section-label`, `.nav-badge`, `.user-role`, `.stat-label`, badges
`.7rem`) and primary UI text at **12–13px** (nav items 12.5px, tenant pill, `th` ~11.5px). Below ~12px is
uncomfortable and (for the 10px tier) an accessibility problem.

**Do — treat this as one global pass, not per-component nudges:**
- Define a **font-size scale as tokens** in `src/styles.css` `:root` (e.g. `--text-xs: 0.75rem` …
  `--text-xl`) with a **12px floor for any UI text** and **15–16px for content** (set `html { font-size:
  16px }` explicitly and prefer `rem`).
- Lift the current tiers: 10–11px → **12–13px**; nav items 12.5px → **~14px**; `th` → **~12px**; table
  body/content → **~15px**; inputs/buttons → **~15px**; `h1` can stay ~1.4rem but verify hierarchy still
  reads after the base bump.
- **Convert px font-sizes to rem** where practical so they scale with the base and respect user zoom.
- Loosen density a notch to match: nav-item padding ~`10px 12px`, `td` padding ~`.8rem`, sidebar width
  `220px → ~240px`. Don't overdo it — aim for "comfortable at 100% zoom," not "spacious."
- Verify in **both themes and RTL**, and confirm the dashboard, tables, and the methodology editor (the
  densest screens) still fit without horizontal scrolling at common widths.

**Acceptance:** the app is comfortable to read at **100% browser zoom**; no UI text below 12px; layouts
don't break in light/dark or RTL.

---

## Backend dependencies (flag to product owner — NOT frontend-only)

These would meaningfully help calibration but require **new .NET API endpoints** before any UI can use
them. They are out of scope for a pure-frontend pass; surface them, don't fake them:

- **Delete factor / question / option in a draft version.** `api.ts` exposes only add/update
  (lines 46–51). Deleting from a *draft* methodology is legitimate (it is not immutable evaluation
  data — LOCKED RULE 4 applies to `evaluation_answer`, not draft authoring). Needs
  `DELETE /methodology-versions/{vid}/factors/{fid}`, `DELETE /factors/{fid}/questions/{qid}`,
  `DELETE /questions/{qid}/options/{oid}`, then matching `api.ts` methods and editor buttons (gated to
  `isDraft()`, with the A2 confirm dialog).
- **Edit / delete grade mappings.** `api.ts` has only `addGradeMapping` (line 52). Fixing a wrong score
  range during calibration currently requires... nothing — you can't edit or remove it. Needs
  `PUT`/`DELETE /methodology-versions/{vid}/grade-mappings/{id}` plus UI.

If/when these endpoints land, wiring the editor buttons is small and follows the existing add/update
patterns in `version-editor.ts`.

---

## Suggested order

1. **E1 readability/type-scale pass** — do this early; it touches global tokens that later tasks build on,
   and it's the most-felt daily improvement during calibration.
2. A1 toast, A2 confirm dialog (everything else leans on them)
3. C1 IA fix — extract `/grading`, rename taxonomy, restructure nav, fix dashboard links (also kills the
   dead-link bug and folds in C2) + C5 workspace switcher (same `app.html` edit). One reviewable change.
4. B1 sort-order bug, B2 grade-mapping validation (calibration correctness)
5. C3 / C4 guardrails, B3 ranked view
6. D1–D4 a11y (C2 already covered by C1)
7. Raise the backend-dependency items with the product owner.
