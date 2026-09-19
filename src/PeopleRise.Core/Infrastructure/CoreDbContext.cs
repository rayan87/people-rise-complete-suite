using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Infrastructure;

/// <summary>Per-tenant database for the Organization Builder core: Organization, Structure, Ladder,
/// Establishment, Roster, and organization Identity (Core Spec §11 - Accounts/AccountRoles, the
/// tenant-local identity system, deliberately unlinked from the control plane's AppUser). Free,
/// mandatory, present in every tenant. No product entity ever lives here - see the Core
/// Specification for the boundary.</summary>
internal class CoreDbContext(DbContextOptions<CoreDbContext> options)
    : IdentityDbContext<Account, AccountRole, Guid>(options)
{
    // Domain-language aliases over Identity's own Users/Roles DbSets.
    public DbSet<Account> Accounts => Users;

    public DbSet<AccountRole> AccountRoles => Roles;

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();

    public DbSet<Level> Levels => Set<Level>();

    public DbSet<JobFamily> JobFamilies => Set<JobFamily>();

    public DbSet<Grade> Grades => Set<Grade>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<JobGradeAssignment> JobGradeAssignments => Set<JobGradeAssignment>();

    public DbSet<JobPosition> JobPositions => Set<JobPosition>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeAssignment> EmployeeAssignments => Set<EmployeeAssignment>();

    public DbSet<SalaryBand> SalaryBands => Set<SalaryBand>();

    public DbSet<PayElement> PayElements => Set<PayElement>();

    public DbSet<EmployeePayElementAmount> EmployeePayElementAmounts => Set<EmployeePayElementAmount>();

    public DbSet<IndustryClassification> IndustryClassifications => Set<IndustryClassification>();

    public DbSet<SeedVersion> SeedVersions => Set<SeedVersion>();

    public DbSet<CompetencyDefinition> CompetencyDefinitions => Set<CompetencyDefinition>();

    public DbSet<CompetencyTemplate> CompetencyTemplates => Set<CompetencyTemplate>();

    public DbSet<CompetencyTemplateItem> CompetencyTemplateItems => Set<CompetencyTemplateItem>();

    public DbSet<RequiredCompetencyOverride> RequiredCompetencyOverrides => Set<RequiredCompetencyOverride>();

    public DbSet<HeldCompetencyProfile> HeldCompetencyProfiles => Set<HeldCompetencyProfile>();

    public DbSet<Certification> Certifications => Set<Certification>();

    public DbSet<CareerPath> CareerPaths => Set<CareerPath>();

    public DbSet<CareerPathStep> CareerPathSteps => Set<CareerPathStep>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);   // Identity's own model: accounts, roles, claims, logins, tokens, user-roles

        // Identity's default table names (AspNetUsers etc.) renamed to match this domain's language.
        // ApplyConventions() below re-snakes whatever name is already set here.
        b.Entity<Account>().ToTable("accounts");
        b.Entity<AccountRole>().ToTable("account_roles");
        b.Entity<IdentityUserRole<Guid>>().ToTable("account_role_assignments");
        b.Entity<IdentityUserClaim<Guid>>().ToTable("account_claims");
        b.Entity<IdentityUserLogin<Guid>>().ToTable("account_logins");
        b.Entity<IdentityUserToken<Guid>>().ToTable("account_tokens");
        b.Entity<IdentityRoleClaim<Guid>>().ToTable("account_role_claims");

        // An account optionally points at an Employee (Core Spec §11: "not every employee has an
        // account... not every account is an employee") - never the reverse, and never cascades.
        b.Entity<Account>().HasOne(x => x.Employee).WithMany()
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        // self-reference
        b.Entity<OrgUnit>().HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId);

        // Job's grade is effective-dated (Core Spec §3.2b/§6) - Job carries no GradeId column.
        b.Entity<JobGradeAssignment>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId);
        b.Entity<JobGradeAssignment>().HasOne(x => x.Grade).WithMany().HasForeignKey(x => x.GradeId);

        // uniqueness
        b.Entity<Level>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Level>().HasIndex(x => x.Rank).IsUnique();
        b.Entity<JobFamily>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Grade>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Grade>().HasIndex(x => x.Rank).IsUnique();
        b.Entity<Job>().HasIndex(x => x.Code).IsUnique();
        b.Entity<JobPosition>().HasIndex(x => x.Code).IsUnique();
        b.Entity<OrgUnit>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Employee>().HasIndex(x => x.EmployeeNo).IsUnique();
        b.Entity<PayElement>().HasIndex(x => x.Code).IsUnique();
        b.Entity<IndustryClassification>().HasIndex(x => x.Code).IsUnique();
        b.Entity<SeedVersion>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Location>().HasIndex(x => x.Code).IsUnique();
        b.Entity<CompetencyDefinition>().HasIndex(x => x.Code).IsUnique();

        // one template per (level, family) cell - JobFamilyId nullable, so the level-only fallback
        // cell needs its own index (Postgres treats NULLs as distinct - see the SalaryBand indexes
        // above for the same split).
        b.Entity<CompetencyTemplate>().HasIndex(x => new { x.LevelId, x.JobFamilyId })
            .IsUnique().HasFilter("job_family_id IS NOT NULL")
            .HasDatabaseName("ix_competency_templates_level_family");
        b.Entity<CompetencyTemplate>().HasIndex(x => x.LevelId)
            .IsUnique().HasFilter("job_family_id IS NULL")
            .HasDatabaseName("ix_competency_templates_level_no_family");

        // one item per competency within a template; one override per job+competency
        b.Entity<CompetencyTemplateItem>().HasIndex(x => new { x.TemplateId, x.CompetencyId }).IsUnique();
        b.Entity<RequiredCompetencyOverride>().HasIndex(x => new { x.JobId, x.CompetencyId }).IsUnique();

        // a grade appears at most once, and at most one step order, within a given path (Core Spec §5)
        b.Entity<CareerPathStep>().HasIndex(x => new { x.CareerPathId, x.GradeId }).IsUnique();
        b.Entity<CareerPathStep>().HasIndex(x => new { x.CareerPathId, x.StepOrder }).IsUnique();

        // at most one OPEN assignment per position, AND per employee (partial unique indexes) -
        // Core Spec §8: "one primary assignment at a time", enforced from both directions.
        b.Entity<EmployeeAssignment>().HasIndex(x => x.PositionId)
            .IsUnique().HasFilter("end_date IS NULL");
        b.Entity<EmployeeAssignment>().HasIndex(x => x.EmployeeId)
            .IsUnique().HasFilter("end_date IS NULL");

        // at most one OPEN grade assignment per job (Core Spec §3.2b: "overlapping windows for the
        // same subject and key are not [permitted]") - the DB-level twin of GradeAssignmentWriter's
        // app-level check.
        b.Entity<JobGradeAssignment>().HasIndex(x => x.JobId)
            .IsUnique().HasFilter("end_date IS NULL");

        // at most one OPEN pay-element amount per employee+element (Core Spec §3.2b), same
        // partial-unique pattern.
        b.Entity<EmployeePayElementAmount>().HasIndex(x => new { x.EmployeeId, x.PayElementId })
            .IsUnique().HasFilter("end_date IS NULL");

        // at most one Published band per grade+family (Core Spec §3.2b). JobFamilyId is nullable
        // and Postgres treats NULLs as distinct in a unique index, so the family-less case needs
        // its own index rather than relying on (GradeId, JobFamilyId) alone.
        b.Entity<SalaryBand>().HasIndex(x => new { x.GradeId, x.JobFamilyId })
            .IsUnique().HasFilter("status = 'Published' AND job_family_id IS NOT NULL")
            .HasDatabaseName("ix_salary_bands_grade_family_published");
        b.Entity<SalaryBand>().HasIndex(x => x.GradeId)
            .IsUnique().HasFilter("status = 'Published' AND job_family_id IS NULL")
            .HasDatabaseName("ix_salary_bands_grade_no_family_published");

        // mirror the DDL's check constraints
        b.Entity<SalaryBand>().ToTable(t =>
            t.HasCheckConstraint("ck_band_order", "max_amount >= midpoint AND midpoint >= min_amount"));

        // snake_case + enums-as-text + money precision + char(3) currency
        b.ApplyConventions();
    }

    // Override the bool overloads so the guard runs on EVERY save path (sync and async).
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    { EfConventions.ApplyTimestampsAndImmutability(ChangeTracker); return base.SaveChanges(acceptAllChangesOnSuccess); }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    { EfConventions.ApplyTimestampsAndImmutability(ChangeTracker); return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct); }
}
