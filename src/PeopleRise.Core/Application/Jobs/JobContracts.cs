namespace PeopleRise.Core.Application.Jobs;

// Job's inherited salary band is back: SalaryBand is now correctly a core entity (Core Spec §9)
// alongside Job/Grade, so it joins into this same query again - no cross-module read involved.
public record JobDto(
    Guid Id, string Code, string TitleEn, string? TitleAr, string? DescriptionEn, string? DescriptionAr,
    Guid? JobFamilyId, string? JobFamilyCode, string? JobFamilyNameEn, string? JobFamilyNameAr,
    Guid? GradeId, string? GradeCode, string? GradeNameEn, string? GradeNameAr,
    Guid? LevelId, string? LevelCode, string? LevelNameEn, string? LevelNameAr,
    string Status, string? GradeSource, JobBandDto? Band);

/// <summary>The salary band the job inherits via its grade (grade-level band). Null until graded/priced.</summary>
public record JobBandDto(string Currency, decimal MinAmount, decimal Midpoint, decimal MaxAmount);
