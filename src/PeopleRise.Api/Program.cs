using Microsoft.EntityFrameworkCore;
using Npgsql;
using PeopleRise.ControlPlane;
using PeopleRise.Modules.JobReward;
using PeopleRise.Tenancy;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddDbContext<ControlPlaneDbContext>(o => o.UseNpgsql(cfg.GetConnectionString("ControlPlane")));
builder.Services.AddTenancy(cfg.GetConnectionString("TenantTemplate")!);
builder.Services.AddJobRewardModule();

// DEV: allow the Angular dev server to call the API with the dev auth headers.
const string DevCors = "dev-spa";
builder.Services.AddCors(o => o.AddPolicy(DevCors, p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors(DevCors);

// Dev bootstrap: create control-plane schema + seed one consultant user and one demo tenant.
await DevBootstrap.RunAsync(app);

app.UseTenancy();   // current-user (dev header) + tenant resolution

app.MapGet("/", () => Results.Ok(new
{
    service = "People Rise API (starter)",
    dev = true,
    howTo = new[]
    {
        "1) GET /me/tenants with header X-User-Id: <dev user id from startup log>",
        "2) Pick a tenant id, then call /levels with X-User-Id AND X-Tenant-Id",
        "3) POST /levels { code, name, rank } to write into that tenant's database"
    }
}));

app.MapGet("/me/tenants", async (ICurrentUser user, ControlPlaneDbContext cp) =>
{
    if (!user.IsAuthenticated) return Results.Unauthorized();
    var rows = await cp.Access.Where(a => a.UserId == user.UserId).Include(a => a.Tenant)
        .Select(a => new { a.TenantId, a.Tenant!.Name, a.Tenant.OwnerType, a.Tenant.Status, a.Role })
        .ToListAsync();
    return Results.Ok(rows);
});

// Provision a new client (a new Model-A engagement): create DB -> schema -> register -> grant access.
app.MapPost("/admin/tenants", async (CreateTenant input, ICurrentUser user,
                                      ControlPlaneDbContext cp, TenantConnectionFactory factory) =>
{
    if (!user.IsAuthenticated) return Results.Unauthorized();
    var dbName = $"pr_tenant_{Guid.NewGuid():N}";
    await Provisioning.CreateDatabaseAsync(cfg.GetConnectionString("Maintenance")!, dbName);
    await JobRewardModule.EnsureSchemaAsync(factory.ForDatabase(dbName));

    var tenant = new Tenant { Name = input.Name, DbName = dbName, OwnerType = input.OwnerType };
    cp.Tenants.Add(tenant);
    cp.Access.Add(new UserTenantAccess { UserId = user.UserId, TenantId = tenant.Id, Role = AccessRole.Consultant });
    await cp.SaveChangesAsync();
    return Results.Created($"/admin/tenants/{tenant.Id}", new { tenant.Id, tenant.Name, tenant.DbName });
});

// Provision a brand-new "El-Delta" client tenant and seed it with realistic demo data
// (Egyptian IT company, ~150–250 staff). Idempotent by name: returns the existing tenant if present.
app.MapPost("/admin/demo/el-delta", async (ICurrentUser user, ControlPlaneDbContext cp, TenantConnectionFactory factory) =>
{
    if (!user.IsAuthenticated) return Results.Unauthorized();

    var existing = await cp.Access.Include(a => a.Tenant)
        .FirstOrDefaultAsync(a => a.UserId == user.UserId && a.Tenant!.Name == "El-Delta");
    if (existing is not null)
        return Results.Ok(new
        {
            tenantId = existing.TenantId,
            name = "El-Delta",
            note = "El-Delta already exists for this user; returning it (not reseeded).",
            dbName = existing.Tenant!.DbName,
        });

    var dbName = $"pr_tenant_{Guid.NewGuid():N}";
    await Provisioning.CreateDatabaseAsync(cfg.GetConnectionString("Maintenance")!, dbName);
    var conn = factory.ForDatabase(dbName);
    await JobRewardModule.EnsureSchemaAsync(conn);
    var summary = await JobRewardModule.SeedElDeltaDemoAsync(conn);

    var tenant = new Tenant { Name = "El-Delta", DbName = dbName, OwnerType = OwnerType.Client };
    cp.Tenants.Add(tenant);
    cp.Access.Add(new UserTenantAccess { UserId = user.UserId, TenantId = tenant.Id, Role = AccessRole.Consultant });
    await cp.SaveChangesAsync();

    return Results.Created($"/admin/tenants/{tenant.Id}", new
    {
        tenant.Id,
        name = tenant.Name,
        industry = "Information Technology",
        companySize = "150-250",
        tenant.DbName,
        seeded = summary,
    });
});

app.MapJobRewardEndpoints();

app.Run();


// ---- dev helpers (replace auth + provisioning hardening for production) ----

static class Provisioning
{
    public static async Task CreateDatabaseAsync(string maintenanceConnectionString, string dbName)
    {
        await using var conn = new NpgsqlConnection(maintenanceConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";   // dbName is from Guid:N (hex only) - safe
        await cmd.ExecuteNonQueryAsync();
    }
}

static class DevBootstrap
{
    public static readonly Guid DevUserId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    public static async Task RunAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var cp = sp.GetRequiredService<ControlPlaneDbContext>();
        await cp.Database.EnsureCreatedAsync();

        if (!await cp.Users.AnyAsync(u => u.Id == DevUserId))
        {
            cp.Users.Add(new AppUser { Id = DevUserId, Email = "dev@peoplerise.local", DisplayName = "Dev Consultant" });
            await cp.SaveChangesAsync();
        }

        if (!await cp.Tenants.AnyAsync())
        {
            var factory = sp.GetRequiredService<TenantConnectionFactory>();
            var maintenance = app.Configuration.GetConnectionString("Maintenance")!;
            var dbName = $"pr_tenant_{Guid.NewGuid():N}";
            await Provisioning.CreateDatabaseAsync(maintenance, dbName);
            await JobRewardModule.EnsureSchemaAsync(factory.ForDatabase(dbName));

            var tenant = new Tenant { Name = "Demo Client", DbName = dbName, OwnerType = OwnerType.Consulting };
            cp.Tenants.Add(tenant);
            cp.Access.Add(new UserTenantAccess { UserId = DevUserId, TenantId = tenant.Id, Role = AccessRole.Consultant });
            await cp.SaveChangesAsync();
            app.Logger.LogInformation("Seeded demo tenant {TenantId} (db {Db})", tenant.Id, dbName);
        }

        app.Logger.LogInformation("DEV USER ID (use as X-User-Id header): {UserId}", DevUserId);
    }
}

public record CreateTenant(string Name, OwnerType OwnerType = OwnerType.Consulting);
