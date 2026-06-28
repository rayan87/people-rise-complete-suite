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
C-level; C-level is out of evaluation scope).

## Tech stack

- Backend: **.NET 10**, **Minimal API**, C#
- ORM/DB: **EF Core 10 + Npgsql**, **PostgreSQL**
- Frontend: **Angular** (not in this repo yet; backend first)
- Solution file: `PeopleRise.slnx` (XML solution format)

## Solution structure (modular monolith)

```
src/
  PeopleRise.SharedKernel       Entity/ImmutableEntity bases, EfConventions
  PeopleRise.ControlPlane       Platform DB: Tenant, AppUser, UserTenantAccess
  PeopleRise.Tenancy            ITenantContext, middleware, connection factory
  PeopleRise.Modules.JobReward  THE Phase 1 module (entities internal; 23 tables)
    Domain/                     Structure.cs, Evaluation.cs, Salary.cs, Enums.cs
    Infrastructure/             JobRewardDbContext, JobRewardModule (public surface)
    Application/                <-- evaluation engine goes here (to be created)
  PeopleRise.Api                Minimal-API host, dev seeding, provisioning
```

Dependency direction: `SharedKernel ← ControlPlane ← Tenancy ← JobReward ← Api`. Never invert it.

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
- The starter currently uses `EnsureCreated` for dev convenience. Migrations are the production path —
  prefer generating migrations over expanding `EnsureCreated`.

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

## Domain model — key concepts

- **Job vs Position vs Employee** are distinct. A *Job* is a role definition (you evaluate it). A
  *Position* (`JobPosition`) is a seat the establishment counts (status `ApprovedVacant` = the "open
  box"). An *Employee* is a person (you pay them). Never collapse these.
- **Job families are nullable** on `Job` and assigned in the design phase. A job needs only a *level* to
  be evaluated; family can come later. No feature may assume family is set.
- **Audit trail**: `EvaluationAnswer` rows (immutable, with `PointsSnapshot`) are the record of why a
  job got its grade. This traceability is the product in a consulting sale.
- **Standalone vs integrated**: each product works alone and gets richer with siblings present. Own your
  data, pull from siblings if present, fall back gracefully if not.
- **Titles are aliases; the score is identity.** Job titles differ across companies for the same work;
  the evaluation score is the content-based comparison key. A cross-company *reference job* layer is
  **deferred to the benchmark phase** — do not build it now. A `reference_job_id` is a future additive
  column.

## Compensation rules (Farouk's methodology — authoritative)

- **Spread = (max / min) − 1.** Default **67%** → min = 75% of midpoint, max = 125% of midpoint.
- **Overlap = (midpoint current / midpoint previous) − 1** (i.e. midpoint progression). Default **25%.**
- Midpoint = arithmetic mean of min and max. Because progression (25%) < spread (67%), adjacent bands
  overlap — intended. These are editable starting points, tunable per grade.
- **Market data binds to the JOB** (the unit the market prices). Family + level are fallback grains when
  a survey is coarser. A position inherits its band via job → grade → band; only the employee has actual
  pay (→ compa-ratio = pay / midpoint; below 75% = below scale).
- Percentiles in market data: P25 / P50 (median) / P75 / P90. Positioning: match ≈ P50, lead ≈ P75, lag ≈ P25.
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

## CURRENT TASK — the evaluation engine

Build the scoring engine in `PeopleRise.Modules.JobReward/Application` and wire endpoints via the module's
public surface (`JobRewardModule.MapJobRewardEndpoints`). Keep `JobRewardDbContext` internal.

Behaviour:
1. Read a `MethodologyVersion` and its `Factor` → `Question` → `AnswerOption` (points) and `GradeMapping`.
2. Given a `Job` + the chosen `AnswerOption` per question:
   - per-factor subtotal = sum of selected answer points (**first cut: plain sum, factor weight = 1.0**;
     leave `Factor.Weight` in the model for a later weighted mode),
   - total score = sum of subtotals,
   - recommended grade = the `GradeMapping` row for that version whose `[MinScore, MaxScore]` contains the total.
3. Persist, all server-side:
   - `Evaluation` (pins `MethodologyVersionId`, stores `TotalScore`, `RecommendedGradeId`, status),
   - `EvaluationAnswer` rows (immutable; `PointsSnapshot` = the points at answering time),
   - `EvaluationFactorScore` rows (per-factor subtotals).
4. Endpoints (suggested):
   - `POST /evaluations` — create a draft for a job + methodology version,
   - `POST /evaluations/{id}/answers` — submit chosen answers, compute, persist (status → Submitted),
   - `GET  /evaluations/{id}` — result + factor breakdown + audit trail,
   - `POST /evaluations/{id}/approve` — status → Approved.

Acceptance check before UI work (the **calibration gate**): scoring 6–10 known El-Delta jobs must rank them
in the expected seniority order. Add a small endpoint or test that runs a set of jobs and lists score → rank.

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
