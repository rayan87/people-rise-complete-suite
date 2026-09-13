# CLAUDE.md — People Rise

Project memory for Claude Code. Read this fully before editing. It encodes locked architecture and
rules that must not be violated. When a request conflicts with a rule here, stop and flag it.

## What this is

People Rise is a modular HCM/HR-tech SaaS suite for the Egyptian market, sold à la carte. Every
product stands alone and gets richer alongside its siblings.

**Phase 1 ships four things:**

1. **Organization Builder (core)** — free, mandatory, present in every tenant. The customer's own
   org chart, jobs, grades, positions, roster, pay, and competency framework.
2. **Job Evaluation** — score a job against a granted methodology → recommended grade, with an
   immutable audit trail.
3. **Compensation** — price grades into bands from market data; compa-ratio and equity once real
   pay is present.
4. **Methodology & Competency Designer** — the control-plane authoring tool.

**Phase 1 is a fan, not a pipeline.** Job Evaluation and Compensation each depend only on the core,
never on each other. A customer can buy either alone. Do not build, name, or document anything that
implies one requires the other.

First customer / pilot: **El-Delta** (five levels: Blue Collar → IC → Supervisory → Managerial →
C-level).

## Document set and authority

- `People Rise — Product Portfolio & Strategy` — what the platform is, what sells alone, phasing.
- `People Rise — Core Specification` — **the contract for the core.** Boundary tests, provenance,
  effective dating, what each core area holds, the read/write/event contract. **Authoritative for
  anything touching core entities.**
- `People Rise — Core Specification, Companion for Farouk` — plain-language version, no schema.
- `People_Rise_Build_Approach` — **superseded.** Its two surviving rules are in this file.

This file gives instructions. The specifications give contracts. **Where this file and the Core
Specification disagree about core behaviour, the specification wins** — and this file should be
fixed. Do not restate spec content here; point at it.

## Tech stack

- Backend: **.NET 10**, **Minimal API**, C#
- ORM/DB: **EF Core 10 + Npgsql**, **PostgreSQL**
- Frontend: **Angular** with **Angular Material (Material Design 3)** — being rebuilt from scratch;
  see *Frontend* below
- Solution file: `PeopleRise.slnx` (XML solution format)

## Solution structure (modular monolith)

```
src/
  PeopleRise.SharedKernel       Entity/ImmutableEntity bases, EfConventions
  PeopleRise.ControlPlane       Platform DB: Tenant, AppUser, UserTenantAccess
  PeopleRise.Tenancy            ITenantContext, middleware, connection factory
  PeopleRise.Core               <-- TARGET: the Organization Builder (to be extracted)
  PeopleRise.Modules.JobReward  Job Evaluation + Compensation (entities internal)
  PeopleRise.Api                Minimal-API host, dev seeding, provisioning
```

Dependency direction: `SharedKernel ← ControlPlane ← Tenancy ← Core ← Modules ← Api`.
Never invert it. Modules depend on Core; Core never depends on a module.

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
- Connection strings: `src/PeopleRise.Api/appsettings.Development.json`.
- Package versions are pinned to `10.0.0`; if restore fails, bump to the latest installed `10.x`.
- `EnsureCreated` is a dev convenience. Migrations are the production path — prefer generating
  migrations over expanding `EnsureCreated`.

## Dev auth & tenancy (how a request works)

There is no real auth yet. Dev stand-ins:
- `X-User-Id` header → the current user (seeded dev user: `00000000-0000-0000-0000-0000000000a1`).
- `X-Tenant-Id` header → the active tenant. Resolved and access-checked by
  `TenantResolutionMiddleware`, which binds the per-request connection string.

DB-per-tenant routing is real: a tenant DbContext is bound to whatever connection `ITenantContext`
resolved. **Background jobs and migration runners have no request, so they must set `ITenantContext`
explicitly before using a tenant DbContext** (it throws otherwise).

## LOCKED RULES — do not violate

Architectural decisions, not preferences. Breaking them is a bug.

1. **DB-per-tenant. No `tenant_id` columns anywhere.** The database boundary *is* the tenant
   boundary. The control-plane DB holds the tenant registry, access grants, and the practice library.
2. **One organization = one tenant = one database.** Anything above the organization (a holding
   company, a ministry) is a control-plane concern and never appears in a tenant database.
3. **Module entities are `internal`.** Modules must not reference each other's types. Cross-module
   communication is events or a public contract. Do not make entities public to "make it work."
4. **Modules read the core; the core never reads a module.** The core publishes events and
   subscribes to none. An uninstalled product simply has no subscriber.
5. **`ImmutableEntity` rows are insert-only.** Never update or delete. Corrections create a new
   record. The `SaveChanges` guard in `EfConventions` enforces this — do not weaken or bypass it.
6. **Money is exact decimal + explicit currency. Never `float`/`double`/`real`.** `decimal` mapped
   to `numeric(18,4)` and a `char(3)` currency code; `EfConventions` applies this automatically.
7. **UUIDv7 keys** via `Guid.CreateVersion7()`. Customer-facing codes (employee number, job code,
   grade code, unit code) are editable display labels and are **never** foreign keys.
8. **snake_case** schema names applied centrally by `EfConventions.ApplyConventions`. Don't
   hand-name columns. Enums stored **as strings**.
9. **Methodology is data, not code, and is versioned.** Every `Evaluation` pins a
   `MethodologyVersionId` and is never silently re-graded. Do not hardcode factors/questions/points.
10. **Compute scores server-side.** Never trust a client-submitted total or grade.
11. **Provenance is written by exactly one writer per value.** Job Evaluation writes `Evaluated` and
    nothing else; nothing else writes `Evaluated`. A missing provenance is a defect — there is no
    `Unknown`. Provenance is never upgraded in place: a new source means a new record.
12. **Keep queries provider-agnostic** (EF Core LINQ, no raw Postgres-only SQL in hot paths) —
    SQL Server is a real future requirement for government/SOE RFPs. Postgres-specific DDL in
    migrations is fine.
13. **Every customer-visible name and description carries an Arabic and an English form.** Both
    authored; a missing form falls back to the present one rather than blocking the record.
14. **Closure, not deletion.** Anything ever referenced is closed, never deleted.

## The core boundary

**Read the Core Specification before touching a core entity.** Four tests decide placement, in order:
does the fact exist whether or not they bought the product; does it need a request, approver, or
document; would an unbought product need it; would it otherwise have to live in two places.

The core holds: Organization · Structure · Ladder · Establishment · Roster · Pay · Competency spine.

The core holds **no** workflows, approvals, derivations (one exception: the competency gap), market
data, allowance policy rules, fiscal treatment, or personal data beyond structural placement.

**A product may not add a field to a core entity.** If it can't be justified against the four tests,
it lives in the product's own module.

Provenance vocabularies:

| Record | Values |
| --- | --- |
| Job-to-grade assignment | `Evaluated` · `ManuallyAssigned` |
| Salary band | `Designed` · `ManuallyEntered` |
| Competency framework entry | `Seeded` · `Granted` · `TenantAuthored` |
| Held competency profile | `Assessed` · `PerformanceCycle` · `SelfDeclared` · `CertificationDerived` |
| Pay element amount | `Contract` · `Policy` · `Import` · `PayrollRun` · `Manual` |

## Domain model — key concepts

- **Job vs Position vs Employee are distinct and never collapsed.** A *Job* is a role definition
  (you evaluate it). A *Position* is a seat the establishment counts. An *Employee* is a person
  (you pay them).
- **A position is approved against a specific job within a grade.** Its grade is *derived* from the
  job's current grade assignment, never declared beside it. Re-grading a job moves its positions and
  shifts establishment counts per grade — correct behaviour, expected in bulk during a restructuring.
- **An employee always occupies a position.** No hiring into no seat; a person between seats is
  moved, not unassigned.
- **Location is an attribute of the org unit**, and is *copied* to the employee's primary location at
  assignment — stored on the roster, editable per employee. Copying, not linking: changing a unit's
  location does not move anyone already assigned, and the two may legitimately differ.
- **Job families are nullable** and assigned in the design phase. A job needs only a *level* to be
  evaluated. Every family-keyed lookup declares a level-only fallback.
- **The level × family grid is one coordinate system** shared by the grading grid, competency
  templates, and career paths. Never run a parallel scheme.
- **Titles are aliases; the score is identity.** Cross-company reference jobs are deferred to the
  benchmark phase — do not build now.
- **Competency framework versions are recorded, not pinned.** Required and held profiles resolve
  against the *current* framework; the authored version is stored as information only.
- **Audit trail**: `EvaluationAnswer` rows (immutable, with `PointsSnapshot`) are the record of why a
  job got its grade. That traceability is the product in a consulting sale.

## Permissions

Roles and their permissions are **tenant data, configured by the customer** — a tenant administrator
defines roles and grants permissions in settings. Nothing hardcoded, no built-in role names.

The control plane says *who may reach a tenant*; the tenant database says *what they may do inside
it*. Pay amounts, compa-ratio, held competency profiles, assessment results, and burnout signals are
**one sensitivity class, gated together**. A product may narrow, never widen.

## Frontend

The existing Angular app is **being replaced, not refactored.** It has no users, no data, and
nothing durable to preserve, so the usual argument against a rewrite does not apply here. Delete it
and start clean on Angular Material with Material Design 3.

**Salvage before deleting.** Three things are worth reading out of the old app rather than
rediscovering: the API client and its DTO shapes, the `X-User-Id` / `X-Tenant-Id` header plumbing,
and any form that already handles a bilingual field pair. Everything else goes.

### Locked UI constraints

These are consequences of rules elsewhere in this file, not frontend preferences. They are expensive
to retrofit, so they hold from the first component.

1. **RTL is a first-class layout direction, not a late toggle.** Arabic is a primary language, so the
   whole layout mirrors. Use CDK `Directionality` and logical CSS properties throughout
   (`margin-inline-start`, not `margin-left`; `padding-inline`, not `padding-left/right`). A
   component that only works in LTR is broken, not unfinished.
2. **Two separate bilingual mechanisms — do not conflate them.** *Content* (job titles, competency
   names, unit names) is bilingual **data**: both forms are authored by the customer, stored on the
   record, and every editor shows both fields. *Chrome* (buttons, labels, validation messages) is
   **translation** and uses Angular i18n. Never translate content; never store chrome.
3. **A missing language form falls back to the one present.** The UI never blocks a save because the
   second language is empty, and never renders an empty label when the other form exists.
4. **Permission gating is server-side; the UI mirrors it.** Hiding a control is a courtesy, not a
   control — the API is the boundary. Pay amounts, compa-ratio, held profiles, assessment results,
   and burnout signals are one sensitivity class: a view either has the grant or does not render.
5. **The questionnaire screen is generated from methodology data,** never hardcoded. Factors,
   questions, weights, and answer options come from the pinned version the API returns. No screen may
   assume a factor count, a question order, or a scale other than 1–5.
6. **Pickers exclude closed records; historical views still show them.** Closure, not deletion, is
   visible in the UI as exactly this split.
7. **Money always renders with its currency code.** Never a bare number, never a locale-guessed
   symbol.

### Material 3 and Angular conventions

- **Theme once, centrally, with M3 design tokens.** No hardcoded colours, spacing, or type scales in
  component styles. A component that hardcodes a hex value is a bug.
- **Standalone components, signal-based state, typed reactive forms** as the default. Flag it before
  introducing NgModules or a state-management library.
- **Prefer Angular Material components over custom ones.** The suite has thirteen products; a custom
  control is thirteen places to maintain it.
- Keep a feature-per-product folder structure that mirrors the backend modules, so the core's screens
  and a product's screens stay separable the way the modules are.

**The Designer is a separate app, not a screen in this one.** It is a control-plane tool for Farouk —
no tenant, no `X-Tenant-Id`, a different audience, and none of the bilingual-content rules above
apply to its own chrome. Do not fold it into the tenant application.

**Write the frontend's own CLAUDE.md when the project exists**, not before — component structure,
routing, and testing conventions should be written against real code rather than guessed at.

## The control plane

A separate database with its own rules. It holds the tenant registry, user accounts and access
grants, and the practice library (methodologies, competency banks, assessment instruments). It never
holds organization data, and a tenant database never holds anything above the organization.

- **No tenant context.** Control-plane code has no `ITenantContext` and no `X-Tenant-Id`. Anything
  reaching for one is in the wrong project.
- **Tenants carry a type** — `Client`, `Scratch`, `Demo` — so non-client databases can be excluded
  from billing, backups, and future benchmark extracts. Never assume a tenant is a client.
- **Granted content is published into the tenant as an immutable versioned snapshot.** An evaluation
  pins a version that physically exists in its own tenant database; it never reads the control plane
  at runtime. Nothing in the Core UI may require a control-plane read to render — an on-prem tenant
  is one tenant database plus the application.
- **Revoking a grant never deletes published snapshots.** A revocation that cascaded would strip the
  audit trail out of historical evaluations, which is the product. Revocation stops new use; it does
  not reach backwards.
- **Content is offered, never pushed.** No control-plane update — a new methodology version, a later
  seed file — is auto-applied to an existing tenant. Anything the customer has edited is theirs and
  is never overwritten.

### Migrations across the estate

- **The control plane migrates first, then tenants.** Grants and entitlements live there and tenant
  schema may depend on the new shape.
- **`Scratch` and `Demo` tenants migrate first as canaries.**
- **Expand and contract, without exception.** The application and every tenant database cannot
  migrate atomically. Add a nullable column, deploy an application that tolerates both shapes,
  backfill, drop the old column in a later release.
- **No destructive migration without an explicit flag.** Dropping a column across client databases is
  unrecoverable.
- A schema-version registry in the control plane records, per tenant, which migration is applied,
  when, and with what outcome. On-prem tenants lag — assume some are behind.

## Compensation rules (Farouk's methodology — authoritative)

- **Spread = (max / min) − 1.** Default **67%** → min = 75% of midpoint, max = 125% of midpoint.
  Midpoint is the arithmetic mean of min and max. The half-spread is stored per band and editable.
- **Overlap = midpoint progression = (midpoint / previous midpoint) − 1.** Default **25%.** Because
  progression sits below spread, adjacent bands overlap by design.
- **Pay basis is explicit everywhere** (basic / total fixed / total cash). Every band and market data
  point declares which basis it prices; every pay element declares which it counts toward. Comparing
  across two bases is a defect.
- **Actual pay is a set of elements, not a number.** A modest basic with a large allowance share is
  routine in Egypt.
- **Market data binds to the JOB** as primary grain; family, level, and grade are fallbacks.
  Percentiles P25 / P50 / P75 / P90; match ≈ P50, lead ≈ P75, lag ≈ P25.
- **Compa-ratio = pay ÷ midpoint** on the declared basis; below 75% is below scale. HR-and-above only.

## Operating modes (affects tenancy, not schema)

**Supply** (Farouk authors and grants; the customer works in their own tenant) · **Operate**
(consultants hold cross-tenant access and work on the client's behalf — the El-Delta Phase 1
reality) · **Self-serve** (the client's own staff). Moving between modes is an **access grant**, not
a data migration. Same tenant DB, same schema.

## In scope / out of scope for Phase 1

- **IN:** the core, Job Evaluation, Compensation, the Designer. Integrated compa-ratio/equity when
  real salaries arrive via CSV snapshot. The competency spine's Phase 1 slice is framework, required
  profiles, and family/level templates — held profiles need Assessment (Phase 4).
- **OUT (foundation only — do not build):** promotion workflow, live payroll integration,
  Performance/Goals/Analytics, cross-company benchmark and reference jobs.

## Two standing rules

1. **Core extraction comes before the evaluation engine grows further.** Every endpoint added to
   JobReward now is something that must later be sorted into core-or-product, and the cost rises with
   size. Provenance columns land in the same pass — both are Phase 1 items the portfolio says must
   not be deferred.
2. **Calibration is a go/no-go gate, permanently.** Before any UI work on the methodology: score
   6–10 known El-Delta jobs and confirm they rank in the expected seniority order. If the ranking is
   wrong, tune weights, publish a new version, re-test. A system producing grades people can't defend
   burns trust faster than a late release.

## CURRENT TASK — core extraction

Create `PeopleRise.Core` and move the core entities out of `PeopleRise.Modules.JobReward`, per the
Core Specification.

1. Move Organization, Structure (units, locations, levels, families, jobs), Ladder (grades,
   job-to-grade assignment), Establishment (positions, occupancy), and Roster into `PeopleRise.Core`
   with its own `CoreDbContext` (still one tenant database).
2. Add provenance columns to grade assignment, salary band, competency entries, held profiles, and
   pay amounts. No `Unknown` value; a writer that can't name its source is wrong.
3. Leave evaluations, answers, factor scores, and methodology content in JobReward. Exactly one fact
   crosses into the core: *this job sits at this grade, assigned this way, from this date.*
4. Provisioning path: ship the competency seed (5–8 bilingual behavioural competencies, editable) and
   the ISIC Rev. 4 industry list (section + division, **not** editable, `Other` + free text escape)
   as versioned data files, with each seed version recorded per tenant.
5. Entitlement service wired at module registration, returning true for everything for now. The core
   never consults it.
6. Keep DbContexts internal; expose each module's surface through its `*Module` registration class.

**Then:** finish the evaluation engine in `PeopleRise.Modules.JobReward/Application` — read
methodology version → factor → question → answer options, compute per-factor subtotals (first cut:
plain sum, weight 1.0) and a total, map to a grade via `GradeMapping`, persist `Evaluation`,
`EvaluationAnswer` (immutable, with `PointsSnapshot`), and `EvaluationFactorScore`. Endpoints:
`POST /evaluations`, `POST /evaluations/{id}/answers`, `GET /evaluations/{id}`,
`POST /evaluations/{id}/approve`. Then the calibration gate.

## Conventions for working in this repo

- Match existing style: primary constructors on DbContexts, expression-bodied members where clear,
  file-scoped namespaces, nullable enabled.
- New tenant-DB tables: add the entity to the relevant `Domain/*.cs`, a `DbSet` on the right
  DbContext, configure relationships in `OnModelCreating`, then generate a migration. Rely on
  `EfConventions` for naming, enums, and money — don't duplicate that config per-entity.
- Don't add public types to a module unless the host genuinely needs them.
- When changing an existing file, prefer minimal, reviewable diffs.
- Run `dotnet build` after changes; fix warnings you introduce. Don't suppress the immutability or
  money rules.
- **When this file grows past one product**, split it: a root CLAUDE.md with the stack, locked rules,
  and dependency direction, plus one per module (`src/PeopleRise.Core/CLAUDE.md`, etc.). Do this at
  the extraction, not before.

If something here is ambiguous or seems to conflict with the code, ask before guessing — these rules
are deliberate and expensive to get wrong.
