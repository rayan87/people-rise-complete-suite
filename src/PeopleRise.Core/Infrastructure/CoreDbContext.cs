using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Infrastructure;

/// <summary>Per-tenant database for the Organization Builder core: Organization, Structure, Ladder,
/// Establishment, Roster. Free, mandatory, present in every tenant. No product entity ever lives
/// here - see the Core Specification for the boundary.</summary>
internal class CoreDbContext(DbContextOptions<CoreDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<Level> Levels => Set<Level>();
    public DbSet<JobFamily> JobFamilies => Set<JobFamily>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeAssignment> EmployeeAssignments => Set<EmployeeAssignment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // self-reference
        b.Entity<OrgUnit>().HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId);

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

        // at most one OPEN assignment per position (partial unique index)
        b.Entity<EmployeeAssignment>().HasIndex(x => x.PositionId)
            .IsUnique().HasFilter("end_date IS NULL");

        // snake_case + enums-as-text + money precision + char(3) currency
        b.ApplyConventions();
    }

    // Override the bool overloads so the guard runs on EVERY save path (sync and async).
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    { EfConventions.ApplyTimestampsAndImmutability(ChangeTracker); return base.SaveChanges(acceptAllChangesOnSuccess); }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    { EfConventions.ApplyTimestampsAndImmutability(ChangeTracker); return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct); }
}
