using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Application.Permissions;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.PayElementAmounts;

// Core Spec §11.2's write-contract table lists Core UI as a legitimate writer of per-employee pay
// element amounts today (alongside Personnel/Compensation/Payroll/adapter, none of which exist yet)
// - a person typing or importing a fact the organization already knows, provenance Manual. This is
// the one command for that row; Personnel/Compensation/Payroll would each get their own command with
// their own provenance value when those products exist (Core Spec §3.1: "a product writes only its
// own provenance value").
public sealed record SetPayElementAmountCommand(Guid EmployeeId, Guid PayElementId, decimal Amount, string Currency, DateOnly? EffectiveDate = null);

internal sealed class SetPayElementAmountHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<SetPayElementAmountCommand, Result<PayElementAmountDto>>
{
    public async Task<Result<PayElementAmountDto>> Handle(SetPayElementAmountCommand cmd, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == cmd.EmployeeId, ct))
            return Error.NotFound("Employee not found.");

        var payElement = await db.PayElements.FirstOrDefaultAsync(p => p.Id == cmd.PayElementId, ct);
        if (payElement is null) return Error.NotFound("Pay element not found.");

        if (string.IsNullOrWhiteSpace(cmd.Currency))
            return Error.Validation("Currency is required.");

        var effectiveDate = cmd.EffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Effective-dated, no overlapping windows for the same employee+element (Core Spec §3.2b) -
        // the same pattern GradeAssignmentWriter uses for job grade assignment.
        var current = await db.EmployeePayElementAmounts
            .Where(a => a.EmployeeId == cmd.EmployeeId && a.PayElementId == cmd.PayElementId && a.EndDate == null)
            .FirstOrDefaultAsync(ct);

        if (current is not null && effectiveDate <= current.EffectiveDate)
        {
            return Error.Validation(
                "The new effective date must be after the employee's current amount for this element - overlapping windows aren't permitted.");
        }

        current?.End(effectiveDate);

        var amount = EmployeePayElementAmount.Create(cmd.EmployeeId, cmd.PayElementId, cmd.Amount, cmd.Currency, effectiveDate, PayProvenance.Manual);
        db.EmployeePayElementAmounts.Add(amount);
        await db.SaveChangesAsync(ct);

        await events.PublishAsync(new PayElementAmountChanged(cmd.EmployeeId, cmd.PayElementId), ct);

        return new PayElementAmountDto(amount.Id, amount.EmployeeId, amount.PayElementId, payElement.Code, payElement.NameEn,
            amount.Amount, amount.Currency, amount.EffectiveDate, amount.EndDate, amount.Provenance.ToString());
    }
}

internal static class SetPayElementAmountEndpoint
{
    public static void MapSetPayElementAmountEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{employeeId:guid}/pay-element-amounts", async (Guid employeeId, SetPayElementAmountRequest body,
                SetPayElementAmountHandler h, CancellationToken ct) =>
            (await h.Handle(new SetPayElementAmountCommand(employeeId, body.PayElementId, body.Amount, body.Currency, body.EffectiveDate), ct)).ToHttp())
            .RequirePermission(Permission.ManageSensitiveData);
    }
}
