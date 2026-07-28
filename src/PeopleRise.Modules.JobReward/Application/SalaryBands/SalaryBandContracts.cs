namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

/// <summary>The salary structure is shown per grade: every grade, with its band if one exists.</summary>
public record SalaryBandRowDto(
    Guid GradeId, string GradeCode, string GradeNameEn, string? GradeNameAr, int Rank, string? LevelCode,
    SalaryBandInfo? Band);

public record SalaryBandInfo(
    Guid Id, string Currency, decimal MinAmount, decimal Midpoint, decimal MaxAmount,
    decimal SpreadPct, decimal? OverlapPct, DateOnly EffectiveDate, string Status);

// Request bodies. Provide exactly one of Midpoint/OverlapPct — the other is derived from the
// previous grade's midpoint (OverlapPct is unusable for the first grade; leave it null there).
public record CreateSalaryBandRequest(Guid GradeId, string Currency, decimal? Midpoint, decimal? OverlapPct, DateOnly EffectiveDate);
public record UpdateSalaryBandRequest(string Currency, decimal? Midpoint, decimal? OverlapPct, DateOnly EffectiveDate);
public record GenerateBandsRequest(decimal BaseMidpoint, decimal ProgressionPct, string Currency, DateOnly EffectiveDate);
