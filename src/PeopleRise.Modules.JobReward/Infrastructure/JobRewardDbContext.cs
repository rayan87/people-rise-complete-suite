using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Infrastructure;

/// <summary>Per-tenant database for Job Evaluation + Compensation (core entities - Organization,
/// Structure, Ladder, Establishment, Roster - live in PeopleRise.Core's CoreDbContext; this context
/// never references them, per LOCKED RULE 4).</summary>
internal class JobRewardDbContext(DbContextOptions<JobRewardDbContext> options) : DbContext(options)
{
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
    // Salary (market data / positioning only - SalaryBand itself lives in PeopleRise.Core)
    public DbSet<MarketDataSnapshot> MarketDataSnapshots => Set<MarketDataSnapshot>();
    public DbSet<MarketDataPoint> MarketDataPoints => Set<MarketDataPoint>();
    public DbSet<BandPositioningPolicy> BandPositioningPolicies => Set<BandPositioningPolicy>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // uniqueness
        b.Entity<MethodologyVersion>().HasIndex(x => new { x.MethodologyId, x.VersionNo }).IsUnique();
        b.Entity<GradeMapping>().HasIndex(x => new { x.MethodologyVersionId, x.GradeId }).IsUnique();
        b.Entity<EvaluationAnswer>().HasIndex(x => new { x.EvaluationId, x.QuestionId, x.AnswerOptionId }).IsUnique();

        // mirror the DDL's check constraints
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
