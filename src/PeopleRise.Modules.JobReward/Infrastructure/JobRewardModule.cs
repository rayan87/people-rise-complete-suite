using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleRise.Modules.JobReward.Application.Demo;
using PeopleRise.Modules.JobReward.Application.Evaluations;
using PeopleRise.Modules.JobReward.Application.Grades;
using PeopleRise.Modules.JobReward.Application.JobFamilies;
using PeopleRise.Modules.JobReward.Application.Jobs;
using PeopleRise.Modules.JobReward.Application.Levels;
using PeopleRise.Modules.JobReward.Application.Methodologies;
using PeopleRise.Modules.JobReward.Application.SalaryBands;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;
using PeopleRise.Tenancy;

namespace PeopleRise.Modules.JobReward;

// Public surface of the module. Entities + DbContext + handlers stay internal; the host only sees these.
public static class JobRewardModule
{
    /// <summary>Registers the per-tenant DbContext, the scoring service, and every command/query
    /// handler in this assembly (scanned — new handlers need no registration).</summary>
    public static IServiceCollection AddJobRewardModule(this IServiceCollection s)
    {
        s.AddDbContext<JobRewardDbContext>((sp, opt) =>
        {
            // sp is the request scope; ITenantContext was set by the tenancy middleware.
            var tenant = sp.GetRequiredService<ITenantContext>();
            opt.UseNpgsql(tenant.ConnectionString);
        });
        s.AddScoped<ScoringService>();
        s.AddHandlersFromAssembly(typeof(JobRewardModule).Assembly);
        return s;
    }

    /// <summary>Creates the Phase 1 schema in a freshly provisioned tenant database.
    /// Dev convenience via EnsureCreated; switch to migrations for production (see README).</summary>
    public static async Task EnsureSchemaAsync(string connectionString)
    {
        var opt = new DbContextOptionsBuilder<JobRewardDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new JobRewardDbContext(opt);
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>Populates a freshly-provisioned tenant DB with the El-Delta demo dataset. No request
    /// context, so it builds its own DbContext from the connection string (like EnsureSchemaAsync).</summary>
    public static async Task<DemoSeedSummary> SeedElDeltaDemoAsync(string connectionString)
    {
        var opt = new DbContextOptionsBuilder<JobRewardDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new JobRewardDbContext(opt);
        return await ElDeltaDemoSeeder.SeedAsync(db);
    }

    /// <summary>Maps every submodule's tenant-scoped endpoints.</summary>
    public static IEndpointRouteBuilder MapJobRewardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapLevelEndpoints();
        app.MapGradeEndpoints();
        app.MapJobFamilyEndpoints();
        app.MapJobEndpoints();
        app.MapMethodologyEndpoints();
        app.MapEvaluationEndpoints();
        app.MapSalaryBandEndpoints();
        return app;
    }
}

/// <summary>Row counts written by the El-Delta demo seeder.</summary>
public record DemoSeedSummary(int Levels, int JobFamilies, int Grades, int Jobs, int Evaluations, int SalaryBands);
