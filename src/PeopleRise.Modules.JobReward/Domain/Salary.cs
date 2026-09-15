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
    public Guid? JobFamilyId { get; set; }   // id only - JobFamily lives in PeopleRise.Core
    public Posture Posture { get; set; } = Posture.Match;
    public int TargetPercentile { get; set; } = 50;
    public DateOnly EffectiveDate { get; set; }
}

// SalaryBand moved to PeopleRise.Core.Domain (Core Spec §9: bands are core, with provenance
// Designed/ManuallyEntered - the same shape as grade assignment). SalaryImportBatch and
// EmployeeCompensation (a single BaseSalary decimal per employee) are removed outright - the
// Decision Log (§10, superseded framings) names this exact shape as replaced by "a pay element
// catalogue plus effective-dated per-employee amounts, in Core" (see Core.Domain.Pay).
