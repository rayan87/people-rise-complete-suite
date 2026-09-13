namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

/// <summary>The salary structure is shown per grade: every grade, with its band if one exists.</summary>
public record SalaryBandRowDto(
    Guid GradeId, string GradeCode, string GradeNameEn, string? GradeNameAr, int Rank, string? LevelCode,
    SalaryBandInfo? Band);

public record SalaryBandInfo(
    Guid Id, string Currency, decimal MinAmount, decimal Midpoint, decimal MaxAmount,
    decimal HalfSpreadPct, decimal SpreadPct, decimal? OverlapPct, DateOnly EffectiveDate, string Status,
    string Provenance);

// Request bodies. Provide exactly one of Midpoint/OverlapPct — the other is derived from the
// previous grade's midpoint (OverlapPct is unusable for the first grade; leave it null there).
// HalfSpreadPct defaults to 25% when omitted (Core Spec §9: stored per band, editable).
public record CreateSalaryBandRequest(Guid GradeId, string Currency, decimal? Midpoint, decimal? OverlapPct, DateOnly EffectiveDate, decimal? HalfSpreadPct = null);
public record UpdateSalaryBandRequest(string Currency, decimal? Midpoint, decimal? OverlapPct, DateOnly EffectiveDate, decimal? HalfSpreadPct = null);
public record GenerateBandsRequest(decimal BaseMidpoint, decimal ProgressionPct, string Currency, DateOnly EffectiveDate);
