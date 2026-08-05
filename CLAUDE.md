# CLAUDE.md — People Rise (Phase 1)

Project memory for Claude Code. Read this fully before editing. It encodes locked architecture
and rules that must not be violated. When a request conflicts with a rule here, stop and flag it.

## What this is

People Rise is a modular HCM/HR-tech SaaS suite for the Egyptian market. **Phase 1** is the
**Job & Reward Design** trio, sold consulting-led:

1. **Job Evaluation** — score a job via a point-factor questionnaire → recommended grade.
2. **Grading Structure** — organize evaluated jobs into a level × family grade grid.
3. **Salary Builder** — price grades into salary bands; (integrated) compa-ratio + equity.

Pipeline: a job comes in, a defensible salary band comes out. Everything down to the band is
**design-time and needs no employee data** — that is what lets it sell as a consulting engagement.

First customer / pilot: **El-Delta** (five levels: Blue Collar → IC → Supervisory → Managerial →
C-level).

## Tech stack

- Backend: **.NET 10**, **Minimal API**, C#
- ORM/DB: **EF Core 10 + Npgsql**, **PostgreSQL**
- Frontend: **Angular 20**, at `frontend/people-rise-web` (standalone components, signals, no NgModules)
- Solution file: `PeopleRise.slnx` (XML solution format)

## Solution structure (modular monolith)

```
src/
  PeopleRise.SharedKernel       Entity/ImmutableEntity bases, EfConventions
  PeopleRise.ControlPlane       Platform DB: Tenant, AppUser, UserTenantAccess
  PeopleRise.Tenancy            ITenantContext, middleware, connection factory
  PeopleRise.Modules.JobReward  THE Phase 1 module (entities internal; 23 tables)
    Domain/                     Structure.cs, Evaluation.cs, Salary.cs, JobEvaluation/, Enums.cs
    Infrastructure/             JobRewardDbContext, JobRewardModule (public surface)
    Application/                Evaluations/, Methodologies/, SalaryBands/, Jobs/, Grades/, Levels/,
                                 JobFamilies/, Demo/ (ElDeltaDemoSeeder) — vertical-slice, folder-per-entity
  PeopleRise.Api                Minimal-API host, dev seeding, provisioning
```

Dependency direction: `SharedKernel ← ControlPlane ← Tenancy ← JobReward ← Api`. Never invert it.

## Frontend architecture (`frontend/people-rise-web/src/app/`, verified against actual code)

```
core/       Api (single HttpClient wrapper, api.ts — every backend call goes through it),
            Session (signals: userId/tenantId/tenants, session.ts), sessionInterceptor (the ONLY place
            X-User-Id/X-Tenant-Id headers are set, session.interceptor.ts), I18n (custom en/ar dict +
            RTL toggle, i18n.ts), Theme (light/dark), Toast, Confirm (promise-based confirm dialog),
            models.ts (hand-written mirror of backend DTOs, bilingual xxxEn/xxxAr fields throughout),
            config.ts (API_BASE hardcoded to http://localhost:5080, dev user id — no environment files)
features/   dashboard/ (stat tiles + pipeline strip + recent-evaluations + module shortcuts, read-only),
            jobs/ (list+filter+create/edit) + job-detail (manual grade assignment via Job.AssignGrade,
            "+ Evaluate" link, per-job evaluation history),
            grading/ (READ-ONLY Level × JobFamily matrix of graded jobs; no mutation — links into
            job-detail.ts for grading actions),
            methodology/ (list, detail, version-editor — the methodology builder, see below),
            evaluation/ (hub = list/sort all evaluations, new-evaluation = pick job + Active version,
            evaluation-detail = questionnaire + submit + score breakdown + approve),
            salary/ (grade grid with editable midpoint/overlap cascade + GenerateBands auto-generate
            form — implements the compensation rules above),
            settings/ (theme/lang toggle + demo-seed button) + settings-structure (the ONLY place with
            Level/Grade/JobFamily CRUD — 3-tab UI)
```

**Conventions actually followed by the code** (not aspirational — verified):
- Every backend call goes through the single `Api` service (`core/api.ts`); no component calls
  `HttpClient` directly except `Session.loadTenants()` (identity bootstrap, not domain data). `Api`
  methods carry zero header logic — dev auth headers are centralized entirely in `sessionInterceptor`.
- All cross-cutting state (`Session`, `I18n`, `Theme`, `Toast`, `Confirm`) is a `providedIn: 'root'`
  service holding Angular `signal`s synced to `localStorage`/DOM via `effect()` — no NgRx, no RxJS
  `Subject`s. `localStorage` keys are consistently `pr.*` (`pr.userId`, `pr.tenantId`, `pr.lang`, `pr.theme`).
  `Api`/`Session` calls are converted to Promises via `firstValueFrom`, so features use `async/await`.
- Every bilingual field pair (`nameEn/nameAr`, `titleEn/titleAr`, `labelEn/labelAr`, ...) is resolved for
  display via `I18n.name(en, ar)` (Arabic only when `lang === 'ar'` AND non-empty, English otherwise) —
  do this for any new bilingual field rather than inlining a ternary.
- **Every literal piece of UI text must go through `i18n.t('key')`** (or `i18n.name(en, ar)` for entity
  fields) — never a bare string in a template, a `title`/`aria-label` attribute, a `ConfirmService.confirm({title, body})` call, or a `?? 'fallback'` error/toast string. Backend enum values shown as badges (evaluation/job/
  methodology-version status) go through `I18n.status(s)` — a helper that looks up `status.<lowercase>` —
  not raw `{{ x.status }}`; the raw value still drives the CSS class. Two exceptions left deliberately
  untranslated: the "People Rise" brand name and the "English"/"العربية" language-picker labels (both are
  proper nouns/autonyms). When adding new UI text, grep for `i18n\.t\(\s*'([a-zA-Z0-9_.]+)'\s*\)` call
  sites against the dictionary's key set to catch typos/missing keys before they ship (a one-line Node
  script, not a build step).
- Ownership is cleanly separated by screen: structural CRUD (Level/Grade/JobFamily) only in
  `settings-structure.ts`; salary-band CRUD only in `salary.ts`; `grading.ts` is read-only/navigational.
  Don't duplicate mutation logic for these across screens.
- All routes are lazy-loaded (`loadComponent`) via `app.routes.ts`; there are currently **no route
  guards** — any tenant/auth gating happens inside components (e.g. checking `session.hasTenant()`),
  not at the router level.

## Build / run / migrate

```bash
dotnet restore PeopleRise.slnx
dotnet build PeopleRise.slnx
dotnet run --project src/PeopleRise.Api          # seeds dev user + a demo tenant on first run

# EF tooling (install once): dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> \
  --project src/PeopleRise.Modules.JobReward --startup-project src/PeopleRise.Api \
  --context JobRewardDbContext
dotnet ef migrations add <Name> \
  --project src/PeopleRise.ControlPlane --startup-project src/PeopleRise.Api \
  --context ControlPlaneDbContext
```

- Prereqs: .NET 10 SDK, a local PostgreSQL (superuser/createdb-capable).
- Connection strings: `src/PeopleRise.Api/appsettings.Development.json` (ControlPlane, TenantTemplate, Maintenance).
- Package versions are pinned to `10.0.0`; if restore fails, bump to the latest installed `10.x`.
- Tenant schema is now migration-based: `JobRewardModule.EnsureSchemaAsync` runs
  `Database.MigrateAsync()` (no more `EnsureCreatedAsync` path), and a first migration (`InitialCreate`,
  23 tables) exists under `src/PeopleRise.Modules.JobReward/Migrations/`. Generating it needed
  `JobRewardDbContextFactory` (`IDesignTimeDbContextFactory<JobRewardDbContext>`, a placeholder
  connection string) — without it, `dotnet ef` resolves the context by building the full `PeopleRise.Api`
  host, which runs `DevBootstrap`'s live-DB side effects (real `CREATE DATABASE`/schema/seed calls) just
  to generate a migration file. `ControlPlaneDbContext` still uses `EnsureCreatedAsync` (unconditionally,
  every startup, in `DevBootstrap.RunAsync`) — not yet migrated, on purpose, smaller scope than JobReward.
- `GET /admin/migrate-dbs` iterates every tenant in the control plane, calls `EnsureSchemaAsync`
  (i.e. migrate) per tenant, and returns a per-tenant `{name, dbName, success, error}` array — one
  failing tenant doesn't abort the run or hide which tenant failed.

## Dev auth & tenancy (how a request works)

There is no real auth yet. Dev stand-ins:
- `X-User-Id` header → the current user (seeded dev user: `00000000-0000-0000-0000-0000000000a1`).
- `X-Tenant-Id` header → the active tenant (a client org). Resolved + access-checked by
  `TenantResolutionMiddleware`, which binds the per-request connection string.

The DB-per-tenant routing is real: `JobRewardDbContext` is bound to whatever connection
`ITenantContext` resolved for the request. **Background jobs / migration runners have no request, so
they must set `ITenantContext` explicitly before using the tenant DbContext** (it throws otherwise).

## LOCKED RULES — do not violate

These are architectural decisions, not preferences. Breaking them is a bug.

1. **DB-per-tenant. No `tenant_id` columns anywhere.** The database boundary *is* the tenant boundary.
   Control-plane DB is separate and holds the tenant registry + access grants.
2. **The tenant is always the client organization** (in both operating models — see below). Access is
   granted via `UserTenantAccess` rows in the control plane.
3. **Module entities are `internal`.** Other modules must not reference JobReward types. Cross-module
   communication is via events or a public contract only. Do not make entities public to "make it work."
4. **`evaluation_answer` (and other `ImmutableEntity` rows) are insert-only.** Never update or delete.
   Corrections create a NEW evaluation. The `SaveChanges` guard in `EfConventions` enforces this — do
   not weaken or bypass it.
5. **Money is exact decimal + explicit currency. Never `float`/`double`/`real`.** Use `decimal` mapped
   to `numeric(18,4)` and a `char(3)` currency code. `EfConventions` applies this automatically for
   `decimal` properties and properties named `Currency`.
6. **UUIDv7 keys** via `Guid.CreateVersion7()` (set in the entity base). Don't switch to int identity.
7. **snake_case** schema names are applied centrally by `EfConventions.ApplyConventions`. Don't
   hand-name columns; rely on the convention. Enums are stored **as strings**.
8. **Methodology is data, not code, and is versioned.** Every `Evaluation` pins a `MethodologyVersionId`.
   Re-tuning publishes a new version; already-scored evaluations keep resolving against their pinned
   version and are NEVER silently re-graded. Do not hardcode factors/questions/points in C#.
9. **Compute scores server-side.** Never trust a client-submitted total or grade.
10. **Keep queries provider-agnostic** (EF Core LINQ, no raw Postgres-only SQL in hot paths) — SQL Server
    is a real future requirement for government/SOE RFPs. Postgres-specific DDL is fine in migrations.
11. **`Job` has no direct `LevelId`.** Level flows transitively via `Job.Grade.LevelId` (`Grade.LevelId`
    is required — every grade belongs to exactly one level). A job's level is therefore unresolvable
    (`null` in DTOs) until the job has a `GradeId` — i.e. **a job isn't part of the organization's graded
    population until it's been graded**, whether via an approved evaluation or a direct manual assignment
    (`Job.AssignGrade`). This is intentional, not a gap: don't add a `LevelId` back onto `Job`, and don't
    add validation that requires a level before a job can be created or evaluated. There is also no
    system-enforced eligibility gate blocking evaluation of any particular level (e.g. C-level) — keeping
    jobs like that out of the questionnaire is a consultant/process decision, not something the code
    checks.

## Domain model — key concepts

- **Job vs Position vs Employee** are distinct. A *Job* is a role definition (you evaluate it). A
  *Position* (`JobPosition`) is a seat the establishment counts (status `ApprovedVacant` = the "open
  box"). An *Employee* is a person (you pay them). Never collapse these.
- **Job families are nullable** on `Job` and assigned in the design phase. A job needs neither family nor
  level to be evaluated — level itself only becomes resolvable once the job is graded (see LOCKED RULES).
  No feature may assume family is set.
- **Audit trail**: `EvaluationAnswer` rows (immutable, with `RatingSnapshot` — the chosen 1–5 rating frozen
  at answering time; points are recomputed on read from the pinned version's weights, never stored) are the
  record of why a job got its grade. This traceability is the product in a consulting sale.
- **Standalone vs integrated**: each product works alone and gets richer with siblings present. Own your
  data, pull from siblings if present, fall back gracefully if not.
- **Titles are aliases; the score is identity.** Job titles differ across companies for the same work;
  the evaluation score is the content-based comparison key. A cross-company *reference job* layer is
  **deferred to the benchmark phase** — do not build it now. A `reference_job_id` is a future additive
  column.

## Compensation rules (Farouk's methodology — authoritative)

- **Min/Max are fixed to the midpoint, not user input.** Min = midpoint − 25%, Max = midpoint + 25%. The
  25% is a hardcoded constant (`SalaryBand.FixedHalfSpreadPct`) — there is no field anywhere to edit it.
- **Spread is a derived OUTPUT, never an input.** Spread = (max / min) − 1. Given the fixed ±25% rule this
  is always ≈**67%** — it is display-only, computed on the fly, and is not a persisted/settable column.
- **Overlap is also a derived OUTPUT**, not stored as a raw pass-through: Overlap = (midpoint of this grade
  / midpoint of the previous grade, by `Grade.Rank`) − 1. The first grade has no previous grade, so its
  overlap is `null` — its overlap input is dimmed in the UI; only its midpoint is editable.
- **Overlap and midpoint are two editable views of the same relationship, for grade 2+:**
  editing overlap sets `midpoint = previousMidpoint + previousMidpoint × overlap`; editing midpoint
  recomputes `overlap = midpoint / previousMidpoint − 1`. Whichever one the user edits, the OTHER is
  recalculated and persisted — the backend never trusts a client-supplied overlap as-is, it always
  re-derives it from the two midpoints. **Editing a grade's midpoint cascades upward**: every later grade
  (by rank) keeps its own stored overlap fixed and gets its midpoint re-derived off the new value below
  it, rippling all the way up the ladder. The cascade stops at the first grade with no existing band yet
  (a gap breaks the chain, same as a null overlap would).
- **Auto-generate bands grid** (`GenerateBands`): takes a Base Midpoint and a **Grade progression %**
  (25% is the frontend's default value, not server-enforced — the backend takes whatever `ProgressionPct`
  it's given; same knob as per-grade overlap, there is no separate "spread" input here either). The first
  grade seeds from the base midpoint; each subsequent grade's midpoint = previous grade's (already-rounded)
  midpoint × (1 + progression), rounded to the nearest 100. Because min/max are fixed at ±25% of midpoint
  and default progression (25%) < spread (~67%), adjacent bands overlap — intended.
- **Market data / percentiles — data model only, not yet wired up.** `MarketDataSnapshot`, `MarketDataPoint`
  (grains: job / family / level, `P25`/`P50`/`P75`/`P90`) and `BandPositioningPolicy` exist as entities and
  `DbSet`s, and `SalaryBand` has `SourceSnapshotId`/`PositioningId` columns, but there is **no
  `Application/MarketData` folder, no handlers, no endpoints** yet — nothing reads or writes them. Treat
  the rules below as the target design to build against, not current behavior:
  - Market data binds to the JOB (the unit the market prices). Family + level are fallback grains when
    a survey is coarser. A position inherits its band via job → grade → band; only the employee has actual
    pay (→ compa-ratio = pay / midpoint; below 75% = below scale).
  - Percentiles: P25 / P50 (median) / P75 / P90. Positioning: match ≈ P50, lead ≈ P75, lag ≈ P25.
- `compa-ratio`, burnout, and any sensitive views are **HR-and-above only** — enforce in permissions, never
  expose to the employee.

## Operating model (affects tenancy, not schema)

Start **Model A** (People Rise is the consultant's internal tool; consultants hold cross-tenant access and
produce deliverables for client orgs) → evolve to **Model B** (the client org's own staff log in). The
A→B transition is an **access grant** (insert `UserTenantAccess` rows), **not a data migration**. The
tenant DB and schema are identical in both.

## In scope / out of scope for Phase 1

- IN: the three design tools above, single-tenant, design-time. Integrated compa-ratio/equity when real
  salaries are imported via CSV snapshot.
- OUT (foundation only — do not build): **promotion workflow** (needs Performance ratings + personnel data,
  Phase 2/3), live payroll integration, Performance/Goals/Analytics, cross-company benchmark + reference jobs.

## Evaluation & methodology engine — built (authoritative scoring rules)

The scoring engine described here superseded an earlier "first cut" (plain sum, factor weight = 1.0) —
that first cut no longer exists in code. This is the real, currently-implemented model; treat it as
locked the same way the compensation rules above are locked. Lives in
`PeopleRise.Modules.JobReward/Application/{Evaluations,Methodologies}`, wired via
`JobRewardModule.MapJobRewardEndpoints`; `JobRewardDbContext` stays internal.

**Methodology authoring** (`MethodologyVersion`, `Factor`, `Question`, `AnswerOption`, `GradeMapping`):
- `MethodologyVersion` has a points budget, `MinPoints`/`MaxPoints` (defaults 200/1000), settable via
  `SetPointBudget` while still in Draft.
- Every `Factor` carries a **weight in %**; every `Question` carries a **weight in %** relative to its
  factor. Points are *calculated*, never entered directly: `Factor points = MaxPoints × Factor.Weight/100`,
  `Question points = Factor points × Question.Weight/100`.
- `AnswerOption` no longer carries arbitrary points — every question uses the same unified **1–5 rating**
  (`AnswerOption.Rating`). `Question score = Question points × (rating / 5)`.
- A `Question` can be optional or required. If an optional question is left unanswered, its points are
  redistributed **equally** across the other questions in the same factor before scoring (temporary, for
  that evaluation's scoring only — never mutates the stored `Question.Weight`).
- `MethodologyVersion.Publish()` validates: factor weights sum to 100% (±0.01), each factor's question
  weights sum to 100%, ≥1 factor with ≥1 question each, ≥1 grade mapping, every grade mapping has both
  `MinScore`/`MaxScore` set. Only then can the version go Active.
- Grade assignment is a two-step flow: `AssignGrade` (attaches a grade with no range yet) → then either
  `SetGradeMappingRange` (manual min/max) or `AutoAssignGradeRanges` (tiles the version's assigned grades
  continuously across `[MinPoints, MaxPoints]` with no gaps/overlap, distributing the integer remainder
  across the first bands so ranges stay inclusive-integer and exact).
- Methodology versions can be exported to / imported from an Excel workbook (`MethodologyWorkbook`,
  5 sheets: Version/Factors/Questions/AnswerOptions/GradeMappings). **Import always creates a new Draft
  version** — it never edits an existing one.

**Evaluation lifecycle** (`Evaluation`, `EvaluationAnswer`, `EvaluationFactorScore`):
- Status: `Draft → Submitted → Approved`, plus `Superseded` (an older Approved evaluation for the same job
  is superseded when a newer one is approved). Approving stamps the recommended grade onto the `Job`
  (`Job.AssignGrade(gradeId, GradeSource.Evaluated)`) — evaluation approval is how a job normally becomes
  graded (see LOCKED RULE 11).
- `EvaluationAnswer` (immutable) stores `RatingSnapshot` (the chosen 1–5 rating, frozen). Points are
  recomputed on read from the pinned version's weights, never stored — keeps the audit trail correct even
  though weights live outside the answer row.
- `EvaluationFactorScore` (immutable) persists the per-factor subtotal.
- All computed server-side via the shared `ScoringService` (used by both submit and calibrate, so the
  rules live once) — client-submitted totals/grades are never trusted (LOCKED RULE 9).

**Endpoints** (all under `/evaluations`, `/methodologies`, `/methodology-versions`, plus
factor/question/answer-option sub-routes) — beyond simple CRUD, the notable ones: `POST /evaluations/{id}/answers`
(submit + score), `POST /evaluations/{id}/approve`, `GET /evaluations/{id}` (result + factor breakdown +
audit trail), `POST /evaluations/calibrate` (exists but unused — see below), `PUT
/methodology-versions/{id}/point-budget`, `POST /methodology-versions/{id}/publish`, grade-mapping range
endpoints (manual + auto), and version import/export.

## Methodology version builder features
- Methodology version import/export: "Export" downloads the
  workbook in xlsx; "Import from Excel" in uploads via API
  (multipart `FormData`) and navigates to the new Draft version.
- Methodology version can be duplicated.
- Methodology version factor can be duplicated including their questions and answers into the same methodology version.
- Methodology version question can be duplicated including their answers into the same factor.

## Conventions for working in this repo

- Match existing style: primary constructors on DbContexts, expression-bodied members where clear, file-scoped
  namespaces, nullable enabled.
- New tenant-DB tables: add the entity to the relevant `Domain/*.cs`, a `DbSet` on `JobRewardDbContext`,
  configure relationships in `OnModelCreating`, then generate a migration. Rely on `EfConventions` for naming,
  enums, money — don't duplicate that config per-entity.
- Don't add public types to a module unless the host genuinely needs them; keep the surface in `JobRewardModule`.
- When changing an existing file, prefer minimal, reviewable diffs.
- Run `dotnet build` after changes; fix warnings you introduce. Don't suppress the immutability/money rules.

## Reference docs (if present in the repo or shared)

- `People_Rise_Product_Scope` — product catalog, GTM, locked decisions, system architecture.
- `People_Rise_Build_Approach` — phasing + the M0–M7 milestone checklist (currently at M5).
- `People_Rise_Phase1_Detailed_Design` — the buildable spec for the three modules (workflows, data model).
- `people_rise_phase1_schema.sql` — the hand-written DDL the EF model mirrors.

If something here is ambiguous or seems to conflict with the code, ask before guessing — the rules above are
deliberate and expensive to get wrong.
