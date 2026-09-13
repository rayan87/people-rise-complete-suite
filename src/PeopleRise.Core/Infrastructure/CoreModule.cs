using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleRise.Core.Application.Demo;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Core.Application.JobFamilies;
using PeopleRise.Core.Application.Jobs;
using PeopleRise.Core.Application.Levels;
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
    public static async Task AssignJobGradesAsync(string connectionString, IReadOnlyDictionary<Guid, Guid> gradeIdByJobId)
    {
        if (gradeIdByJobId.Count == 0) return;

        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new CoreDbContext(options);
        var jobIds = gradeIdByJobId.Keys.ToList();
        var jobsById = await dbContext.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id);

        foreach (var (jobId, gradeId) in gradeIdByJobId)
            jobsById[jobId].AssignGrade(gradeId, GradeSource.Evaluated);

        await dbContext.SaveChangesAsync();
    }

    /// <summary>Maps every submodule's tenant-scoped endpoints.</summary>
    public static IEndpointRouteBuilder MapCoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapLevelEndpoints();
        app.MapGradeEndpoints();
        app.MapJobFamilyEndpoints();
        app.MapJobEndpoints();
        return app;
    }
}
