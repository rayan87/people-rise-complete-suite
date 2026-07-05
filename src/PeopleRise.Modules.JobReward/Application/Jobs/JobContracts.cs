namespace PeopleRise.Modules.JobReward.Application.Jobs;

public record JobDto(
    Guid Id, string Code, string TitleEn, string? TitleAr, string? DescriptionEn, string? DescriptionAr,
    Guid LevelId, string? LevelCode, string? LevelNameEn, string? LevelNameAr,
    Guid? JobFamilyId, string? JobFamilyCode, string? JobFamilyNameEn, string? JobFamilyNameAr,
    Guid? GradeId, string? GradeCode, string? GradeNameEn, string? GradeNameAr,
    string Status, string? GradeSource, JobBandDto? Band);

/// <summary>The salary band the job inherits via its grade (grade-level band). Null until graded/priced.</summary>
public record JobBandDto(string Currency, decimal MinAmount, decimal Midpoint, decimal MaxAmount);
