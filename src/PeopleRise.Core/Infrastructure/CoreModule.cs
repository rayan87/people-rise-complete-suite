using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleRise.Core.Application.Competencies;
using PeopleRise.Core.Application.Demo;
using PeopleRise.Core.Application.Employees;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Core.Application.IndustryClassifications;
using PeopleRise.Core.Application.JobFamilies;
using PeopleRise.Core.Application.Jobs;
using PeopleRise.Core.Application.Levels;
using PeopleRise.Core.Application.Locations;
using PeopleRise.Core.Application.OrgUnits;
using PeopleRise.Core.Application.Organizations;
using PeopleRise.Core.Application.PayElements;
using PeopleRise.Core.Application.Permissions;
using PeopleRise.Core.Application.Positions;
using PeopleRise.Core.Application.Provisioning;
using PeopleRise.Core.Application.SalaryBands;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;
using PeopleRise.Tenancy;

namespace PeopleRise.Core;

// Public surface of the Organization Builder core. Entities + DbContext + handlers stay internal;
// the host and every module only see this. See PeopleRise.Core.Application.* for the public
// command/query/DTO contract each submodule exposes.
public static class CoreModule
{
    /// <summary>Registers the per-tenant CoreDbContext and every command/query handler in this
    /// assembly (scanned - new handlers need no registration).</summary>
    public static IServiceCollection AddCoreModule(this IServiceCollection s)
    {
        s.AddDbContext<CoreDbContext>((serviceProvider, options) =>
        {
            // sp is the request scope; ITenantContext was set by the tenancy middleware.
            var tenant = serviceProvider.GetRequiredService<ITenantContext>();
            options.UseNpgsql(tenant.ConnectionString);
        });
        s.AddHandlersFromAssembly(typeof(CoreModule).Assembly);

        // Cross-cutting contracts every module can use (Core Spec §11.3/§11.4). Registered here,
        // once, since Core is the foundation every module already depends on.
        s.AddScoped<IEventPublisher, EventPublisher>();
        s.AddScoped<IEntitlementService, AlwaysEntitledService>();
        s.AddScoped<IPermissionService, PermissionService>();
        return s;
    }

    /// <summary>Creates the core schema in a freshly provisioned tenant database. Dev convenience
    /// via a direct connection string, matching JobRewardModule.EnsureSchemaAsync - no request scope
    /// exists yet during provisioning, so ITenantContext can't be used.</summary>
    public static async Task EnsureSchemaAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new CoreDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>Seeds the versioned reference/provisioning data every tenant gets regardless of what
    /// was bought (Core Spec §4: the ISIC list, "seeded into every tenant at provisioning" - exactly
    /// like the competency seed will be). Idempotent (checked via SeedVersion), so it's safe to call
    /// on every provisioning path, not just brand-new tenants.</summary>
    public static async Task SeedReferenceDataAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new CoreDbContext(options);
        await IsicSeeder.SeedAsync(dbContext);
        await CompetencySeeder.SeedAsync(dbContext);
    }

    /// <summary>Bootstraps a "Full Access" role granted to the given user, for a freshly provisioned
    /// tenant. Not special-cased code - just a normal, fully-editable tenant-owned Role row the
    /// tenant's own administrator can rename, narrow, or reassign afterward (Core Spec §3.6: nothing
    /// hardcoded). Without this, a newly provisioned tenant would have zero roles and every
    /// permission-gated endpoint would 403 for its very first user.</summary>
    public static async Task GrantFullAccessAsync(string connectionString, Guid userId)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new CoreDbContext(options);
        var role = Role.Create("Full Access", "صلاحية كاملة", Enum.GetValues<Permission>());
        dbContext.Roles.Add(role);
        dbContext.RoleAssignments.Add(RoleAssignment.Create(userId, role.Id));
        await dbContext.SaveChangesAsync();
    }

    /// <summary>Populates a freshly-provisioned tenant DB with the El-Delta demo dataset's core
    /// slice (Levels, JobFamilies, Grades, Jobs). Must run before JobReward's own seed phase, which
    /// needs the ids returned here (methodology grade mappings, evaluations, salary bands all key
    /// off core rows). No request context, so it builds its own DbContext like EnsureSchemaAsync.</summary>
    public static async Task<ElDeltaCoreSeedResult> SeedElDeltaDemoAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new CoreDbContext(options);
        return await ElDeltaCoreSeeder.SeedAsync(dbContext);
    }

    /// <summary>Demo/seeding helper: stamps grades onto jobs (source Evaluated) outside the request
    /// pipeline, for the El-Delta demo seed's evaluation phase. Job Evaluation (in
    /// PeopleRise.Modules.JobReward) computes which job earned which grade; this performs the write,
    /// since Job lives here in Core (Core Spec §11.2 - Job Evaluation may write this fact, Core owns
    /// the entity). Not used by the request-time evaluation flow, which calls the public
    /// AssignJobGradeCommand through DI instead - this exists only because seeding has no request
    /// scope to resolve that handler from.</summary>
    public static async Task AssignJobGradesAsync(string connectionString, IReadOnlyDictionary<Guid, Guid> gradeIdByJobId, DateOnly? effectiveDate = null)
    {
        if (gradeIdByJobId.Count == 0) return;

        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new CoreDbContext(options);
        var jobIds = gradeIdByJobId.Keys.ToList();
        var jobsById = await dbContext.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id);
        var effective = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var (jobId, gradeId) in gradeIdByJobId)
        {
            var result = await GradeAssignmentWriter.AssignAsync(dbContext, jobsById[jobId], gradeId, GradeSource.Evaluated, effective, CancellationToken.None);
            if (result.IsFailure) throw new InvalidOperationException(result.Error!.Message);
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>Maps every submodule's tenant-scoped endpoints.</summary>
    public static IEndpointRouteBuilder MapCoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapLevelEndpoints();
        app.MapGradeEndpoints();
        app.MapJobFamilyEndpoints();
        app.MapJobEndpoints();
        app.MapSalaryBandEndpoints();
        app.MapPayElementEndpoints();
        app.MapOrganizationEndpoints();
        app.MapListIndustryClassificationsEndpoint();
        app.MapLocationEndpoints();
        app.MapOrgUnitEndpoints();
        app.MapPositionEndpoints();
        app.MapEmployeeEndpoints();
        app.MapPermissionEndpoints();
        app.MapCompetencyEndpoints();
        return app;
    }
}
