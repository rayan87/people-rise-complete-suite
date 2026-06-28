-- ============================================================================
-- People Rise — Phase 1 schema (Job & Reward Design trio)
-- Target: PostgreSQL 13+  |  Access: Npgsql + EF Core (.NET)
--
-- DEPLOYMENT NOTE (load-bearing): this is ONE TENANT'S database.
-- Under DB-per-tenant there is NO tenant_id column anywhere — the database
-- boundary *is* the tenant boundary. A separate control-plane database (not in
-- this file) holds the tenant registry, billing, and the connection catalog.
-- A third, separate store holds the future anonymized benchmark; only
-- consented, aggregated extracts ever flow there.
--
-- CONVENTIONS
--   * UUID primary keys (gen_random_uuid) — collision-free if rows are ever
--     pooled into the cross-tenant benchmark store later.
--   * timestamptz everywhere; created_at / updated_at on mutable tables.
--   * Money = numeric(18,4) + explicit 3-letter currency. NEVER float.
--   * Status fields = text + CHECK (easy to extend vs. native enum types).
-- ============================================================================

create extension if not exists pgcrypto;   -- provides gen_random_uuid()

-- Shared updated_at trigger (apply to every mutable table).
create or replace function set_updated_at() returns trigger as $$
begin new.updated_at = now(); return new; end;
$$ language plpgsql;


-- ============================================================================
-- SECTION A — ORGANISATION & STRUCTURE (shared spine)
--   Rule of thumb: you EVALUATE jobs, you COUNT positions, you PAY employees.
--   These three must stay separate entities or job_position-control breaks.
-- ============================================================================

-- Org hierarchy (departments / units). Self-referencing tree.
create table org_unit (
    id          uuid primary key default gen_random_uuid(),
    parent_id   uuid references org_unit(id),
    code        text not null unique,
    name        text not null,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);
create index ix_org_unit_parent on org_unit(parent_id);

-- The 5 levels (Blue Collar -> IC -> Supervisory -> Managerial -> C-level).
-- rank orders them vertically. C-level exists here but is OUT of evaluation scope.
create table level (
    id              uuid primary key default gen_random_uuid(),
    code            text not null unique,
    name            text not null,
    rank            int  not null unique,                 -- 1..5, vertical order
    in_eval_scope   boolean not null default true,        -- C-level = false
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

-- Job families = the HORIZONTAL cut across levels (Engineering, Finance, HR...).
-- Populated AFTER go-live; jobs reference it nullably (see job.job_family_id).
create table job_family (
    id          uuid primary key default gen_random_uuid(),
    code        text not null unique,
    name        text not null,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);

-- Grades: the output bucket of evaluation. rank orders them.
create table grade (
    id          uuid primary key default gen_random_uuid(),
    code        text not null unique,
    name        text not null,
    rank        int  not null unique,
    level_id    uuid references level(id),                -- optional grouping into a level
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);

-- JOB = a role DEFINITION ("Senior Accountant"). This is what gets evaluated.
--   * level_id: which of the 5 levels (required).
--   * job_family_id: NULLABLE — a job is fully usable before families exist;
--     it just isn't placed on the horizontal axis yet.
--   * grade_id: NULLABLE — assigned once an evaluation is approved.
create table job (
    id              uuid primary key default gen_random_uuid(),
    code            text not null unique,
    title           text not null,
    description     text,
    level_id        uuid not null references level(id),
    job_family_id   uuid references job_family(id),       -- nullable: add later
    grade_id        uuid references grade(id),            -- nullable: set post-eval
    status          text not null default 'draft'
                       check (status in ('draft','evaluated','active','archived')),
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);
create index ix_job_level   on job(level_id);
create index ix_job_family  on job(job_family_id);
create index ix_job_grade   on job(grade_id);

-- POSITION = a specific SEAT and the unit of the establishment ("...Cairo, slot 3").
--   A vacant approved job_position = the "empty box" a promotion needs.
create table job_position (
    id          uuid primary key default gen_random_uuid(),
    job_id      uuid not null references job(id),
    org_unit_id uuid not null references org_unit(id),
    code        text not null unique,
    status      text not null default 'approved_vacant'
                   check (status in ('approved_vacant','filled','frozen','abolished')),
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);
create index ix_job_position_job    on job_position(job_id);
create index ix_job_position_unit   on job_position(org_unit_id);
create index ix_job_position_status on job_position(status);

-- EMPLOYEE = a person. (Phase 1 keeps this minimal; Personnel product owns the rest.)
create table employee (
    id              uuid primary key default gen_random_uuid(),
    employee_no     text not null unique,
    full_name       text not null,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

-- Which employee fills which job_position over time (historized).
-- Current assignment = end_date IS NULL.
create table employee_assignment (
    id           uuid primary key default gen_random_uuid(),
    employee_id  uuid not null references employee(id),
    position_id  uuid not null references job_position(id),
    start_date   date not null,
    end_date     date,
    created_at   timestamptz not null default now(),
    updated_at   timestamptz not null default now()
);
create index ix_assignment_emp on employee_assignment(employee_id);
create index ix_assignment_pos on employee_assignment(position_id);
-- At most one open (current) assignment per job_position:
create unique index ux_assignment_open_position
    on employee_assignment(position_id) where end_date is null;


-- ============================================================================
-- SECTION B — JOB EVALUATION ENGINE (configurable; methodology = data, not code)
--   Point-factor / "Hay-style" are just seeded rows of these tables.
--   VERSIONING is load-bearing: tuning weights must NOT silently rewrite the
--   grades of jobs already evaluated. Each evaluation pins a methodology_version.
-- ============================================================================

create table methodology (
    id          uuid primary key default gen_random_uuid(),
    code        text not null unique,
    name        text not null,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);

-- A frozen, point-in-time configuration. Calibration back-test -> publish v2;
-- v1 stays intact for every evaluation that already used it.
create table methodology_version (
    id              uuid primary key default gen_random_uuid(),
    methodology_id  uuid not null references methodology(id),
    version_no      int  not null,
    status          text not null default 'draft'
                       check (status in ('draft','active','retired')),
    note            text,
    published_at    timestamptz,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    unique (methodology_id, version_no)
);

-- Factors belong to a version (Know-How, Problem Solving, Accountability...).
create table factor (
    id                      uuid primary key default gen_random_uuid(),
    methodology_version_id  uuid not null references methodology_version(id),
    code                    text not null,
    name                    text not null,
    weight                  numeric(6,3) not null default 1.0,
    sort_order              int not null default 0,
    created_at              timestamptz not null default now(),
    updated_at              timestamptz not null default now(),
    unique (methodology_version_id, code)
);
create index ix_factor_version on factor(methodology_version_id);

-- The job-content questionnaire: questions hang off factors.
create table question (
    id          uuid primary key default gen_random_uuid(),
    factor_id   uuid not null references factor(id),
    question_text text not null,
    help_text   text,
    sort_order  int not null default 0,
    -- loose presentation metadata can live here without schema churn:
    metadata    jsonb not null default '{}'::jsonb,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);
create index ix_question_factor on question(factor_id);

-- Simple-rule mapping (locked decision): each answer option carries fixed points.
create table answer_option (
    id          uuid primary key default gen_random_uuid(),
    question_id uuid not null references question(id),
    label       text not null,
    points      int  not null,                 -- answer X = Y points
    sort_order  int not null default 0,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);
create index ix_answer_option_question on answer_option(question_id);

-- Score range -> grade, per version (this is where score becomes a grade).
create table grade_mapping (
    id                      uuid primary key default gen_random_uuid(),
    methodology_version_id  uuid not null references methodology_version(id),
    grade_id                uuid not null references grade(id),
    min_score               int not null,
    max_score               int not null,
    created_at              timestamptz not null default now(),
    updated_at              timestamptz not null default now(),
    check (max_score >= min_score),
    unique (methodology_version_id, grade_id)
);
create index ix_grade_mapping_version on grade_mapping(methodology_version_id);

-- One evaluation of one job under one methodology version.
create table evaluation (
    id                      uuid primary key default gen_random_uuid(),
    job_id                  uuid not null references job(id),
    methodology_version_id  uuid not null references methodology_version(id),
    evaluator_employee_id   uuid references employee(id),   -- or app user id
    status                  text not null default 'draft'
                               check (status in ('draft','submitted','approved','superseded')),
    total_score             int,
    recommended_grade_id    uuid references grade(id),
    submitted_at            timestamptz,
    approved_at             timestamptz,
    approved_by             uuid references employee(id),
    created_at              timestamptz not null default now(),
    updated_at              timestamptz not null default now()
);
create index ix_evaluation_job     on evaluation(job_id);
create index ix_evaluation_version on evaluation(methodology_version_id);

-- The audit trail: one immutable row per chosen answer.
--   points_snapshot freezes the points at answering time (defence-in-depth even
--   though the version already pins them). Corrections = NEW evaluation, never
--   an UPDATE here. Enforce insert-only in the app / via revoked UPDATE,DELETE.
create table evaluation_answer (
    id              uuid primary key default gen_random_uuid(),
    evaluation_id   uuid not null references evaluation(id),
    question_id     uuid not null references question(id),
    answer_option_id uuid not null references answer_option(id),
    points_snapshot int not null,
    created_at      timestamptz not null default now(),
    unique (evaluation_id, question_id)        -- one answer per question per eval
);
create index ix_eval_answer_eval on evaluation_answer(evaluation_id);

-- Optional cache of per-factor subtotals for an evaluation (derivable from answers).
create table evaluation_factor_score (
    id              uuid primary key default gen_random_uuid(),
    evaluation_id   uuid not null references evaluation(id),
    factor_id       uuid not null references factor(id),
    score           int not null,
    unique (evaluation_id, factor_id)
);


-- ============================================================================
-- SECTION C — SALARY / REWARD
--   Design-time features (market data, bands) need NO employee data — that is
--   why the trio sells consulting-led without a Payroll product. Employee pay
--   (compa-ratio, equity) is the integrated-only layer.
-- ============================================================================

-- An imported market dataset. effective_date carries RECENCY (critical under
-- high inflation). Point-in-time snapshot per the locked sourcing decision.
create table market_data_snapshot (
    id              uuid primary key default gen_random_uuid(),
    name            text not null,
    source          text,                          -- e.g. Mercer / Connectalents / Bayt
    effective_date  date not null,                 -- recency
    currency        char(3) not null,
    note            text,
    imported_at     timestamptz not null default now()
);

-- Individual market reference rows, tagged with the attributes that make them
-- meaningful (geography, industry, size) and percentiles.
create table market_data_point (
    id              uuid primary key default gen_random_uuid(),
    snapshot_id     uuid not null references market_data_snapshot(id),
    job_family_id   uuid references job_family(id),
    level_id        uuid references level(id),
    grade_id        uuid references grade(id),
    geography       text,
    industry        text,
    company_size    text,
    currency        char(3) not null,
    p25             numeric(18,4),
    p50             numeric(18,4),
    p75             numeric(18,4),
    p90             numeric(18,4),
    created_at      timestamptz not null default now()
);
create index ix_mdp_snapshot on market_data_point(snapshot_id);
create index ix_mdp_family   on market_data_point(job_family_id);

-- Positioning posture, set PER FAMILY (El-Delta: match, varying by family).
create table band_positioning_policy (
    id                  uuid primary key default gen_random_uuid(),
    job_family_id       uuid references job_family(id),     -- null = default/all
    posture             text not null default 'match'
                           check (posture in ('lead','match','lag')),
    target_percentile   int not null default 50
                           check (target_percentile between 1 and 100),
    effective_date      date not null,
    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now()
);

-- A salary band for a grade (optionally per family). midpoint comes from market +
-- positioning. Farouk's locked definitions (El-Delta: 67% spread, 25% overlap):
--   spread_pct  = (max / min) - 1                                  as a percent  (default 67)
--   overlap_pct = (midpoint_current / midpoint_previous) - 1       midpoint progression (default 25)
-- With midpoint = arithmetic mean of min/max:
--   min = 2 * midpoint / (2 + spread_pct/100);  max = min * (1 + spread_pct/100)
--   (a 67% spread gives min = 75% of midpoint, max = 125% of midpoint)
create table salary_band (
    id                  uuid primary key default gen_random_uuid(),
    grade_id            uuid not null references grade(id),
    job_family_id       uuid references job_family(id),     -- null = applies to all families
    currency            char(3) not null,
    midpoint            numeric(18,4) not null,
    min_amount          numeric(18,4) not null,
    max_amount          numeric(18,4) not null,
    spread_pct          numeric(5,2) not null default 67.0,   -- (max/min) - 1, percent
    overlap_pct         numeric(5,2) not null default 25.0,   -- midpoint progression to next grade
    source_snapshot_id  uuid references market_data_snapshot(id),
    positioning_id      uuid references band_positioning_policy(id),
    effective_date      date not null,
    status              text not null default 'draft'
                           check (status in ('draft','published','retired')),
    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now(),
    check (max_amount >= midpoint and midpoint >= min_amount)
);
create index ix_band_grade  on salary_band(grade_id);
create index ix_band_family on salary_band(job_family_id);

-- INTEGRATED-ONLY: actual employee pay, for compa-ratio & internal equity.
-- Imported as a point-in-time snapshot via the consulting CSV path (same path
-- reused for the standalone fallback). Sensitive — consent + PDPL handling.
create table salary_import_batch (
    id          uuid primary key default gen_random_uuid(),
    filename    text,
    source      text not null default 'consulting_import'
                   check (source in ('consulting_import','payroll')),
    row_count   int,
    note        text,
    imported_at timestamptz not null default now()
);

create table employee_compensation (
    id              uuid primary key default gen_random_uuid(),
    employee_id     uuid not null references employee(id),
    base_salary     numeric(18,4) not null,
    currency        char(3) not null,
    effective_date  date not null,
    import_batch_id uuid references salary_import_batch(id),
    created_at      timestamptz not null default now()
);
create index ix_emp_comp_emp on employee_compensation(employee_id);
-- compa-ratio is then computed (not stored):
--   employee -> current assignment -> job_position -> job -> grade -> salary_band
--   compa_ratio = base_salary / salary_band.midpoint
--   below 0.75 (75%) = below scale.


-- ============================================================================
-- SECTION D — updated_at triggers (apply to all mutable tables)
-- ============================================================================
do $$
declare t text;
begin
  foreach t in array array[
    'org_unit','level','job_family','grade','job','job_position','employee',
    'employee_assignment','methodology','methodology_version','factor','question',
    'answer_option','grade_mapping','evaluation','band_positioning_policy','salary_band'
  ] loop
    execute format(
      'create trigger trg_%1$s_updated before update on %1$s
       for each row execute function set_updated_at();', t);
  end loop;
end $$;

-- END OF PHASE 1 SCHEMA
