using System.ComponentModel.DataAnnotations.Schema;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class MarketDataSnapshot : ImmutableEntity
{
    public string Name { get; set; } = "";
    public string? Source { get; set; }
    public DateOnly EffectiveDate { get; set; }   // recency
    public string Currency { get; set; } = "";
    public string? Note { get; set; }
}

internal class MarketDataPoint : ImmutableEntity
{
    public Guid SnapshotId { get; set; }
    public MarketDataSnapshot? Snapshot { get; set; }
    public Guid? JobFamilyId { get; set; }
    public Guid? LevelId { get; set; }
    public Guid? GradeId { get; set; }
    public string? Geography { get; set; }
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public string Currency { get; set; } = "";
    public decimal? P25 { get; set; }
    public decimal? P50 { get; set; }
    public decimal? P75 { get; set; }
    public decimal? P90 { get; set; }
}

internal class BandPositioningPolicy : Entity   // per family: lead / match / lag
{
    public Guid? JobFamilyId { get; set; }
    public JobFamily? JobFamily { get; set; }
    public Posture Posture { get; set; } = Posture.Match;
    public int TargetPercentile { get; set; } = 50;
    public DateOnly EffectiveDate { get; set; }
}

internal class SalaryBand : Entity   // min = midpoint-25%, max = midpoint+25% (fixed); spread & overlap are derived outputs
{
    private const decimal FixedHalfSpreadPct = 25m;   // CLAUDE.md: min = midpoint-25%, max = midpoint+25%; not user-editable

    public Guid GradeId { get; private set; }
    public Grade? Grade { get; private set; }
    public Guid? JobFamilyId { get; private set; }
    public JobFamily? JobFamily { get; private set; }
    public string Currency { get; private set; } = "";
    public decimal Midpoint { get; private set; }
    public decimal MinAmount { get; private set; }
    public decimal MaxAmount { get; private set; }
    public decimal? OverlapPct { get; private set; }   // (this midpoint / previous grade's midpoint) - 1; null for the first grade
    public Guid? SourceSnapshotId { get; private set; }
    public Guid? PositioningId { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public BandStatus Status { get; private set; } = BandStatus.Draft;

    [NotMapped]
    public decimal SpreadPct => MinAmount <= 0 ? 0m : (MaxAmount / MinAmount - 1m) * 100m;

    private SalaryBand() { }   // EF

    public static SalaryBand Create(Guid gradeId,
        string currency,
        decimal midpoint,
        decimal? previousGradeMidpoint,
        DateOnly effectiveDate,
        BandStatus status = BandStatus.Published,
        Guid? jobFamilyId = null)
    {
        var band = new SalaryBand
        {
            GradeId = gradeId, JobFamilyId = jobFamilyId, Currency = currency,
            EffectiveDate = effectiveDate, Status = status,
        };
        band.ApplyMidpoint(midpoint, previousGradeMidpoint);
        return band;
    }

    /// <summary>Re-price the band: min/max/overlap are always derived from midpoint (never set directly).</summary>
    public void Update(decimal midpoint, decimal? previousGradeMidpoint, string currency, DateOnly effectiveDate)
    {
        Currency = currency;
        EffectiveDate = effectiveDate;
        ApplyMidpoint(midpoint, previousGradeMidpoint);
    }

    public void Retire() => Status = BandStatus.Retired;

    private void ApplyMidpoint(decimal midpoint, decimal? previousGradeMidpoint)
    {
        Midpoint = midpoint;

        var half = FixedHalfSpreadPct / 100m;
        MinAmount = decimal.Round(midpoint * (1m - half), 4);
        MaxAmount = decimal.Round(midpoint * (1m + half), 4);

        OverlapPct = previousGradeMidpoint is { } prev && prev > 0
            ? decimal.Round((midpoint / prev - 1m) * 100m, 4)
            : null;
    }
}

internal class SalaryImportBatch : ImmutableEntity
{
    public string? Filename { get; set; }
    public CompSource Source { get; set; } = CompSource.ConsultingImport;
    public int? RowCount { get; set; }
    public string? Note { get; set; }
}

internal class EmployeeCompensation : ImmutableEntity   // integrated-only: enables compa-ratio + equity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal BaseSalary { get; set; }
    public string Currency { get; set; } = "";
    public DateOnly EffectiveDate { get; set; }
    public Guid? ImportBatchId { get; set; }
    public SalaryImportBatch? ImportBatch { get; set; }
}
