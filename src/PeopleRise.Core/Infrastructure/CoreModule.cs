using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PeopleRise.Core.Application.CareerPaths;
using PeopleRise.Core.Application.Competencies;
using PeopleRise.Core.Application.Demo;
using PeopleRise.Core.Application.Employees;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Core.Application.Identity;
using PeopleRise.Core.Application.IndustryClassifications;
using PeopleRise.Core.Application.JobFamilies;
using PeopleRise.Core.Application.Jobs;
using PeopleRise.Core.Application.Levels;
using PeopleRise.Core.Application.Locations;
using PeopleRise.Core.Application.OrgUnits;
using PeopleRise.Core.Application.Organizations;
using PeopleRise.Core.Application.PayElements;
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

        // Organization identity (Core Spec §11) - a real ASP.NET Core Identity setup (password
        // hashing/validation, UserManager<Account>, RoleManager<AccountRole>) scoped to CoreDbContext,
        // the same per-request tenant database everything else here uses. AddIdentityCore, not
        // AddIdentity: no cookie/scheme wiring - this only supplies the account/role store and
        // management APIs the Core UI write-contract needs (§12.2), not a login flow. No default
        // token providers either - those back email-confirmation/password-reset tokens, which need
        // IDataProtectionProvider wired at the host; nothing here uses them yet.
        s.AddIdentityCore<Account>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<AccountRole>()
            .AddEntityFrameworkStores<CoreDbContext>();

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

    /// <summary>Provisions the seeded, system-owned Admin AccountRole (Core Spec §11.2: "exists from
    /// provisioning with the full tenant permission set... cannot be edited... cannot be deleted")
    /// if it doesn't already exist, then creates an Account with the given id/email and assigns it.
    /// Without this, a newly provisioned tenant would have zero accounts/roles and every
    /// permission-gated endpoint would 403 for its very first user.
    /// <paramref name="accountId"/> is set explicitly (not left to Account's own UUIDv7 default) so
    /// callers that already know the id they want to authenticate as later (the dev user, a
    /// consultant's platform user id) get a predictable account. The password is a random,
    /// discarded placeholder - no per-tenant login flow exists yet (Core Spec §11.5/§13: adapters
    /// and real authentication are still open), Identity just needs *a* value that satisfies its
    /// password policy to create the row.</summary>
    public static async Task ProvisionAdminAccountAsync(string connectionString, Guid accountId, string email)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new CoreDbContext(options);
        var (users, roles) = CreateIdentityManagers(dbContext);

        var adminRole = await roles.FindByNameAsync("Admin")
            ?? await CreateAdminRoleAsync(roles);

        var account = Account.Create(email);
        account.Id = accountId;
        var createResult = await users.CreateAsync(account, GenerateBootstrapPassword());
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                $"Could not create account {email}: {string.Join(' ', createResult.Errors.Select(e => e.Description))}");

        var assignResult = await users.AddToRoleAsync(account, adminRole.Name!);
        if (!assignResult.Succeeded)
            throw new InvalidOperationException(
                $"Could not grant Admin to {email}: {string.Join(' ', assignResult.Errors.Select(e => e.Description))}");
    }

    private static async Task<AccountRole> CreateAdminRoleAsync(RoleManager<AccountRole> roles)
    {
        var role = AccountRole.Create("Admin", "مسؤول", isSystemOwned: true);
        var createResult = await roles.CreateAsync(role);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                $"Could not create the Admin role: {string.Join(' ', createResult.Errors.Select(e => e.Description))}");

        foreach (var permission in Enum.GetValues<Permission>())
            await roles.AddClaimAsync(role, new System.Security.Claims.Claim(AccountRole.PermissionClaimType, permission.ToString()));

        return role;
    }

    private static string GenerateBootstrapPassword() => $"P{Guid.CreateVersion7():N}!1";

    /// <summary>Builds UserManager/RoleManager by hand, the same reason every other method here
    /// builds its own CoreDbContext: provisioning happens before a tenant exists, so there is no
    /// request scope for DI to resolve the normally-registered (AddIdentityCore, see AddCoreModule)
    /// UserManager&lt;Account&gt;/RoleManager&lt;AccountRole&gt; from.</summary>
    internal static (UserManager<Account> Users, RoleManager<AccountRole> Roles) CreateIdentityManagers(CoreDbContext db)
    {
        var normalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();

        var users = new UserManager<Account>(
            new UserStore<Account, AccountRole, CoreDbContext, Guid>(db),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<Account>(),
            [new UserValidator<Account>()],
            [new PasswordValidator<Account>()],
            normalizer,
            errors,
            services: null!,   // only used to resolve two-factor token providers, which we don't use
            NullLogger<UserManager<Account>>.Instance);

        var roles = new RoleManager<AccountRole>(
            new RoleStore<AccountRole, CoreDbContext, Guid>(db),
            [new RoleValidator<AccountRole>()],
            normalizer,
            errors,
            NullLogger<RoleManager<AccountRole>>.Instance);

        return (users, roles);
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
        app.MapIdentityEndpoints();
        app.MapCompetencyEndpoints();
        app.MapCareerPathEndpoints();
        return app;
    }
}
