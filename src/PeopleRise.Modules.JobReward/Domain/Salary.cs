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

internal class SalaryBand : Entity   // min = mid*(1-spread%); max = mid*(1+spread%); overlap = midpoint progression
{
    public Guid GradeId { get; private set; }
    public Grade? Grade { get; private set; }
    public Guid? JobFamilyId { get; private set; }
    public JobFamily? JobFamily { get; private set; }
    public string Currency { get; private set; } = "";
    public decimal Midpoint { get; private set; }
    public decimal MinAmount { get; private set; }
    public decimal MaxAmount { get; private set; }
    public decimal SpreadPct { get; private set; } = 67m;
    public decimal OverlapPct { get; private set; } = 25m;   // configurable; drives midpoint-to-midpoint progression
    public Guid? SourceSnapshotId { get; private set; }
    public Guid? PositioningId { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public BandStatus Status { get; private set; } = BandStatus.Draft;

    private SalaryBand() { }   // EF

    public static SalaryBand Create(Guid gradeId, string currency, decimal midpoint, decimal spreadPct,
        decimal overlapPct, DateOnly effectiveDate, BandStatus status = BandStatus.Published, Guid? jobFamilyId = null)
    {
        var band = new SalaryBand
        {
            GradeId = gradeId, JobFamilyId = jobFamilyId, Currency = currency,
            SpreadPct = spreadPct, OverlapPct = overlapPct, EffectiveDate = effectiveDate, Status = status,
        };
        band.ApplyMidpoint(midpoint);
        return band;
    }

    /// <summary>Re-price the band: min/max are always derived from midpoint ± spread (never set directly).</summary>
    public void Update(decimal midpoint, decimal spreadPct, decimal overlapPct, string currency, DateOnly effectiveDate)
    {
        SpreadPct = spreadPct;
        OverlapPct = overlapPct;
        Currency = currency;
        EffectiveDate = effectiveDate;
        ApplyMidpoint(midpoint);
    }

    public void Retire() => Status = BandStatus.Retired;

    private void ApplyMidpoint(decimal midpoint)
    {
        // SpreadPct is (max/min − 1), e.g. 67% → min = 75% of midpoint, max = 125% (CLAUDE.md).
        // From midpoint M and spread s: min = 2M/(2+s), max = 2M(1+s)/(2+s); mean(min,max) == M.
        Midpoint = midpoint;
        var s = SpreadPct / 100m;
        MinAmount = decimal.Round(2m * midpoint / (2m + s), 4);
        MaxAmount = decimal.Round(2m * midpoint * (1m + s) / (2m + s), 4);
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
