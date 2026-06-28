# People Rise — Starter Solution (Phase 1 spine)

A runnable .NET 10 / Minimal-API scaffold that proves the **tenancy spine** end to end before any product
features are built: a request resolves to the correct tenant database, access is authorized, and the
load-bearing data rules (insert-only audit rows, exact-decimal money, snake_case schema) hold.

## What's inside

| Project | Role |
|---|---|
| `PeopleRise.SharedKernel` | `Entity` / `ImmutableEntity` bases; shared EF conventions (snake_case, enums-as-text, money precision, timestamps, insert-only guard). |
| `PeopleRise.ControlPlane` | Platform-wide DB: `Tenant`, `AppUser`, `UserTenantAccess` (the cross-tenant grant). |
| `PeopleRise.Tenancy` | `ITenantContext`, dev current-user + tenant-resolution middleware, per-tenant connection factory. |
| `PeopleRise.Modules.JobReward` | The Phase 1 module: 23 entities (internal), `JobRewardDbContext`, public module surface (DI + endpoints + schema init). |
| `PeopleRise.Api` | Minimal-API host: wiring, dev seeding, provisioning, demo endpoints. |

Module entities are `internal` on purpose — other modules **cannot** reference them. The boundary is
compiler-enforced, not just convention.

## Prerequisites
- .NET 10 SDK
- PostgreSQL running locally (a superuser/createdb-capable login; defaults assume `postgres`/`postgres`)

> Package versions are pinned to `10.0.0`. If `dotnet restore` complains, bump to the latest installed `10.x`.

## Configure
Edit `src/PeopleRise.Api/appsettings.Development.json` connection strings:
- **ControlPlane** — the platform DB (created automatically on first run).
- **TenantTemplate** — host/credentials with **no** database; the tenant DB name is appended per request.
- **Maintenance** — points at the `postgres` database; used only to issue `CREATE DATABASE`.

## Run
```bash
dotnet run --project src/PeopleRise.Api
```
On first run it creates the control-plane schema, seeds one consultant user, provisions a **Demo Client**
tenant (new database + Phase 1 schema), and logs the dev user id.

## Try it (dev auth = headers)
```bash
# list tenants you can access (Model A: a consultant sees many)
curl -H "X-User-Id: 00000000-0000-0000-0000-0000000000a1" http://localhost:5080/me/tenants

# read levels in a tenant (use a TenantId from the call above)
curl -H "X-User-Id: 00000000-0000-0000-0000-0000000000a1" -H "X-Tenant-Id: <tenant-id>" \
     http://localhost:5080/levels

# write a level into that tenant's database
curl -X POST -H "X-User-Id: 00000000-0000-0000-0000-0000000000a1" -H "X-Tenant-Id: <tenant-id>" \
     -H "Content-Type: application/json" \
     -d '{"code":"IC","name":"Individual Contributor","rank":2}' \
     http://localhost:5080/levels

# provision another client (a new Model-A engagement)
curl -X POST -H "X-User-Id: 00000000-0000-0000-0000-0000000000a1" \
     -H "Content-Type: application/json" -d '{"name":"Second Client"}' \
     http://localhost:5080/admin/tenants
```

## What to verify (the point of this slice)
1. **Tenant routing** — the same `/levels` endpoint reads/writes a *different* database per `X-Tenant-Id`.
2. **Access control** — a `X-Tenant-Id` you have no grant for returns **403**.
3. **Insert-only** — attempting to update/delete an `EvaluationAnswer` throws (the guard in `EfConventions`).
4. **Exact money** — decimal columns are `numeric(18,4)`, never float.
5. **Model A → B** — a consultant holds many `UserTenantAccess` rows; opening to a client = inserting access
   rows for their users. No data migration.

## Dev shortcuts to replace before production
- **Auth.** `X-User-Id` is a stand-in. Wire real JWT/OIDC; set `ICurrentUser` from the token.
- **Schema.** `EnsureCreated` builds schema fast for dev. For production switch to **EF migrations**:
  ```bash
  dotnet ef migrations add Initial_JobReward \
    --project src/PeopleRise.Modules.JobReward --startup-project src/PeopleRise.Api \
    --context JobRewardDbContext
  ```
  then apply per tenant DB at provisioning time (and run the control-plane context's migrations separately).
- **Background work.** Anything without an HTTP request (jobs, the migration runner) must set
  `ITenantContext` explicitly before using the tenant DbContext — it will throw loudly if you forget.
- **Provisioning.** Harden `CREATE DATABASE` (least-privilege role, retries, teardown on failure).

## Build order that mirrors the architecture
SharedKernel → ControlPlane → Tenancy → JobReward (context + entities) → Api. Get the spine green with the
single seeded tenant first; that's the data-layer equivalent of the calibration gate — validate the hard part
in isolation before stacking product logic on top.
