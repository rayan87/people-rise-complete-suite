namespace PeopleRise.Core.Application.Jobs;

// NOTE: the pre-extraction JobDto carried an inherited-salary-band field, joined in from JobReward's
// SalaryBand table in the same query. The core must never read a module (LOCKED RULE 4), so that
// field is dropped here; a caller wanting "job + its grade's band" composes it from this DTO plus a
// JobReward salary-bands lookup (JobReward is allowed to read the core, just never the reverse).
public record JobDto(
    Guid Id, string Code, string TitleEn, string? TitleAr, string? DescriptionEn, string? DescriptionAr,
    Guid? JobFamilyId, string? JobFamilyCode, string? JobFamilyNameEn, string? JobFamilyNameAr,
    Guid? GradeId, string? GradeCode, string? GradeNameEn, string? GradeNameAr,
    Guid? LevelId, string? LevelCode, string? LevelNameEn, string? LevelNameAr,
    string Status, string? GradeSource);
