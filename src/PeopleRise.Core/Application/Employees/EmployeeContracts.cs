namespace PeopleRise.Core.Application.Employees;

public record EmployeeDto(
    Guid Id, string EmployeeNo, string FullNameEn, string? FullNameAr,
    DateOnly HireDate, string EmploymentStatus, Guid? PrimaryLocationId, string? PrimaryLocationCode,
    Guid? CurrentPositionId, string? CurrentPositionCode, Guid? CurrentJobId, string? CurrentJobTitleEn, string? CurrentOrgUnitCode);

public record HireEmployeeRequest(string EmployeeNo, string FullNameEn, string? FullNameAr, DateOnly HireDate, Guid PositionId);
public record MoveEmployeeRequest(Guid NewPositionId, DateOnly EffectiveDate);
public record SeparateEmployeeRequest(DateOnly SeparationDate);
public record UpdateEmploymentStatusRequest(string Status);
