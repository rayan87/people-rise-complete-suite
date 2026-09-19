namespace PeopleRise.Core.Application.CareerPaths;

public record CareerPathStepDto(Guid GradeId, string GradeCode, string GradeNameEn, string? GradeNameAr, int StepOrder);
public record CareerPathDto(Guid Id, Guid JobFamilyId, string JobFamilyCode, string NameEn, string? NameAr, IReadOnlyList<CareerPathStepDto> Steps);

// GradeIdsInOrder is the path's steps, first to last - StepOrder is assigned from list position.
public record CreateCareerPathRequest(Guid JobFamilyId, string NameEn, string? NameAr, IReadOnlyList<Guid> GradeIdsInOrder);
public record UpdateCareerPathRequest(string NameEn, string? NameAr, IReadOnlyList<Guid> GradeIdsInOrder);
