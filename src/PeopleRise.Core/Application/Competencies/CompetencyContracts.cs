namespace PeopleRise.Core.Application.Competencies;

// ---- Framework ----
public record CompetencyDto(Guid Id, string Code, string NameEn, string? NameAr, string? DescriptionEn, string? DescriptionAr, string Provenance, string Status);
public record CreateCompetencyRequest(string Code, string NameEn, string? NameAr, string? DescriptionEn, string? DescriptionAr);
public record UpdateCompetencyRequest(string Code, string NameEn, string? NameAr, string? DescriptionEn, string? DescriptionAr);

// ---- Templates (the family-and-level cell) ----
public record TemplateItemDto(Guid CompetencyId, string CompetencyCode, string CompetencyNameEn, string? CompetencyNameAr, int RequiredLevel);
public record CompetencyTemplateDto(Guid Id, Guid LevelId, string LevelCode, Guid? JobFamilyId, string? JobFamilyCode, IReadOnlyList<TemplateItemDto> Items);
public record CreateTemplateRequest(Guid LevelId, Guid? JobFamilyId, IReadOnlyList<TemplateItemRequest> Items);
public record TemplateItemRequest(Guid CompetencyId, int RequiredLevel);
public record SetTemplateItemsRequest(IReadOnlyList<TemplateItemRequest> Items);

// ---- Required profile (job-bound, template + overrides) ----
public record RequiredCompetencyItemDto(Guid CompetencyId, string CompetencyCode, string CompetencyNameEn, string? CompetencyNameAr, int RequiredLevel, bool IsOverride);
public record RequiredProfileDto(Guid JobId, IReadOnlyList<RequiredCompetencyItemDto> Items);
public record SetRequiredOverrideRequest(Guid CompetencyId, int? RequiredLevel);   // null = excluded

// ---- Held profile (employee-bound, source-resolved) ----
public record HeldCompetencyItemDto(Guid CompetencyId, string CompetencyCode, string CompetencyNameEn, string? CompetencyNameAr, int Level, string Source);
public record HeldProfileDto(Guid EmployeeId, IReadOnlyList<HeldCompetencyItemDto> Items);
public record RecordSelfDeclaredLevelRequest(Guid CompetencyId, int Level);
public record RecordCertificationRequest(Guid CompetencyId, string NameEn, string? NameAr, int Level, DateOnly IssuedDate);

// ---- Gap (the one computed exception - Core Spec §10) ----
public record CompetencyGapItemDto(Guid CompetencyId, string CompetencyCode, string CompetencyNameEn, string? CompetencyNameAr, int RequiredLevel, int? HeldLevel, int Gap);
public record CompetencyGapDto(Guid EmployeeId, Guid? JobId, IReadOnlyList<CompetencyGapItemDto> Items);
