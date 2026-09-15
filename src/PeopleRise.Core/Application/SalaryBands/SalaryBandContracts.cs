namespace PeopleRise.Core.Application.SalaryBands;

/// <summary>The salary structure is shown per grade: every grade, with its band if one exists.</summary>
public record SalaryBandRowDto(
    Guid GradeId, string GradeCode, string GradeNameEn, string? GradeNameAr, int Rank, string? LevelCode,
    SalaryBandInfo? Band);

public record SalaryBandInfo(
    Guid Id, string Currency, decimal MinAmount, decimal Midpoint, decimal MaxAmount,
    decimal HalfSpreadPct, decimal SpreadPct, decimal? OverlapPct, DateOnly EffectiveDate, string Status,
    string Provenance, string Basis);

// Request bodies. Provide exactly one of Midpoint / OverlapPct / (MinAmount+MaxAmount):
// - Midpoint (+ optional HalfSpreadPct, defaulting 25%): Min/Max derived symmetrically.
// - OverlapPct: Midpoint derived from the previous grade's midpoint (unusable for the first grade).
// - MinAmount+MaxAmount: entered directly (Core Spec §9 - "bands exist without Compensation");
//   Midpoint and the effective half-spread are derived read-only from them, so HalfSpreadPct must
//   be omitted in this mode.
// All three are Core UI's direct-entry path - provenance is always ManuallyEntered (see
// Compensation's SetDesignedBandCommand for the algorithmically-generated path).
public record CreateSalaryBandRequest(Guid GradeId, string Currency, DateOnly EffectiveDate,
    decimal? Midpoint = null, decimal? OverlapPct = null, decimal? HalfSpreadPct = null,
    decimal? MinAmount = null, decimal? MaxAmount = null);
public record UpdateSalaryBandRequest(string Currency, DateOnly EffectiveDate,
    decimal? Midpoint = null, decimal? OverlapPct = null, decimal? HalfSpreadPct = null,
    decimal? MinAmount = null, decimal? MaxAmount = null);
