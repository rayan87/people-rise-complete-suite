namespace PeopleRise.Core.Application.PayElementAmounts;

public record PayElementAmountDto(
    Guid Id, Guid EmployeeId, Guid PayElementId, string PayElementCode, string PayElementNameEn,
    decimal Amount, string Currency, DateOnly EffectiveDate, DateOnly? EndDate, string Provenance);

public record SetPayElementAmountRequest(Guid PayElementId, decimal Amount, string Currency, DateOnly? EffectiveDate = null);
