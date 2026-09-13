using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleRise.Core;
using PeopleRise.Core.Application.Demo;
using PeopleRise.Modules.JobReward.Application.Demo;
using PeopleRise.Modules.JobReward.Application.Evaluations;
using PeopleRise.Modules.JobReward.Application.Methodologies;
using PeopleRise.Modules.JobReward.Application.SalaryBands;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;
using PeopleRise.Tenancy;

namespace PeopleRise.Modules.JobReward;

// Public surface of the module (Job Evaluation + Compensation). Entities + DbContext + handlers
// stay internal; the host only sees these. Structure/Ladder/Establishment/Roster live in
// PeopleRise.Core - see CoreModule for their registration/schema/seed/endpoints.
public static class JobRewardModule
{
    /// <summary>Registers the per-tenant DbContext, the scoring service, and every command/query
    /// handler in this assembly (scanned — new handlers need no registration).</summary>
    public static IServiceCollection AddJobRewardModule(this IServiceCollection s)
    {
        s.AddDbContext<JobRewardDbContext>((serviceProvider, options) =>
        {
            // sp is the request scope; ITenantContext was set by the tenancy middleware.
            var tenant = serviceProvider.GetRequiredService<ITenantContext>();
            options.UseNpgsql(tenant.ConnectionString);
        });
        s.AddScoped<ScoringService>();
        s.AddHandlersFromAssembly(typeof(JobRewardModule).Assembly);
        return s;
    }

    /// <summary>Creates the Phase 1 schema in a freshly provisioned tenant database.
    /// Dev convenience via EnsureCreated; switch to migrations for production (see README).
    /// Call AFTER CoreModule.EnsureSchemaAsync — see the class-level note on why.</summary>
    public static async Task EnsureSchemaAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<JobRewardDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new JobRewardDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>Populates a freshly-provisioned tenant DB with the El-Delta demo dataset's Job
    /// Evaluation + Compensation slice (methodology, evaluations, salary bands). MUST run after
    /// <paramref name="coreSeed"/> was produced by CoreModule.SeedElDeltaDemoAsync — this phase
    /// grades jobs and prices grades that only exist once the core phase has committed them.</summary>
    public static async Task<DemoSeedSummary> SeedElDeltaDemoAsync(string connectionString, ElDeltaCoreSeedResult coreSeed)
    {
        var options = new DbContextOptionsBuilder<JobRewardDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        (DemoSeedSummary Summary, IReadOnlyDictionary<Guid, Guid> GradeAssignments) result;
        await using (var dbContext = new JobRewardDbContext(options))
        {
            result = await ElDeltaDemoSeeder.SeedAsync(dbContext, coreSeed);
        }

        // The job itself lives in PeopleRise.Core - this module computed the outcome, Core performs
        // the write (Core Spec §11.2: Job Evaluation may write the job-to-grade assignment).
        await CoreModule.AssignJobGradesAsync(connectionString, result.GradeAssignments);
        return result.Summary;
    }

    /// <summary>Maps every submodule's tenant-scoped endpoints. Levels/Grades/JobFamilies/Jobs are
    /// mapped by CoreModule.MapCoreEndpoints instead - see Program.cs.</summary>
    public static IEndpointRouteBuilder MapJobRewardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethodologyEndpoints();
        app.MapEvaluationEndpoints();
        app.MapSalaryBandEndpoints();
        return app;
    }
}

/// <summary>Row counts written by the El-Delta demo seeder.</summary>
public record DemoSeedSummary(int Levels, int JobFamilies, int Grades, int Jobs, int Evaluations, int SalaryBands);
