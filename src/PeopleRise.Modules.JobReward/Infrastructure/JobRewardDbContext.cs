using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Infrastructure;

/// <summary>Per-tenant database for the Job & Reward Design trio (the Phase 1 schema).</summary>
internal class JobRewardDbContext(DbContextOptions<JobRewardDbContext> options) : DbContext(options)
{
    // Structure
    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<Level> Levels => Set<Level>();
    public DbSet<JobFamily> JobFamilies => Set<JobFamily>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeAssignment> EmployeeAssignments => Set<EmployeeAssignment>();
    // Evaluation
    public DbSet<Methodology> Methodologies => Set<Methodology>();
    public DbSet<MethodologyVersion> MethodologyVersions => Set<MethodologyVersion>();
    public DbSet<Factor> Factors => Set<Factor>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<GradeMapping> GradeMappings => Set<GradeMapping>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<EvaluationAnswer> EvaluationAnswers => Set<EvaluationAnswer>();
    public DbSet<EvaluationFactorScore> EvaluationFactorScores => Set<EvaluationFactorScore>();
    // Salary
    public DbSet<MarketDataSnapshot> MarketDataSnapshots => Set<MarketDataSnapshot>();
    public DbSet<MarketDataPoint> MarketDataPoints => Set<MarketDataPoint>();
    public DbSet<BandPositioningPolicy> BandPositioningPolicies => Set<BandPositioningPolicy>();
    public DbSet<SalaryBand> SalaryBands => Set<SalaryBand>();
    public DbSet<SalaryImportBatch> SalaryImportBatches => Set<SalaryImportBatch>();
    public DbSet<EmployeeCompensation> EmployeeCompensations => Set<EmployeeCompensation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // self-reference
        b.Entity<OrgUnit>().HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId);

        // disambiguate the (single) Evaluation -> Employee navigation; ApprovedBy is a bare column
        b.Entity<Evaluation>().HasOne(e => e.EvaluatorEmployee).WithMany()
            .HasForeignKey(e => e.EvaluatorEmployeeId).OnDelete(DeleteBehavior.Restrict);

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
        b.Entity<MethodologyVersion>().HasIndex(x => new { x.MethodologyId, x.VersionNo }).IsUnique();
        b.Entity<GradeMapping>().HasIndex(x => new { x.MethodologyVersionId, x.GradeId }).IsUnique();
        b.Entity<EvaluationAnswer>().HasIndex(x => new { x.EvaluationId, x.QuestionId, x.AnswerOptionId }).IsUnique();

        // at most one OPEN assignment per position (partial unique index)
        b.Entity<EmployeeAssignment>().HasIndex(x => x.PositionId)
            .IsUnique().HasFilter("end_date IS NULL");

        // mirror the DDL's check constraints
        b.Entity<SalaryBand>().ToTable(t =>
            t.HasCheckConstraint("ck_band_order", "max_amount >= midpoint AND midpoint >= min_amount"));
        b.Entity<GradeMapping>().ToTable(t =>
            t.HasCheckConstraint("ck_grade_mapping_score", "max_score >= min_score"));

        // snake_case + enums-as-text + money precision + char(3) currency
        b.ApplyConventions();
    }

    // Override the bool overloads so the guard runs on EVERY save path (sync and async).
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    { EfConventions.ApplyTimestampsAndImmutability(ChangeTracker); return base.SaveChanges(acceptAllChangesOnSuccess); }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    { EfConventions.ApplyTimestampsAndImmutability(ChangeTracker); return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct); }
}
