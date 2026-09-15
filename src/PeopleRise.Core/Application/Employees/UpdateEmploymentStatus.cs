using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Employees;

// Active <-> OnLongTermAbsence only - Separated has position side-effects and only happens through
// SeparateEmployeeCommand (Core Spec §8: "employment status is structural, not procedural").
public sealed record UpdateEmploymentStatusCommand(Guid EmployeeId, string Status);

internal sealed class UpdateEmploymentStatusHandler(CoreDbContext db)
    : ICommandHandler<UpdateEmploymentStatusCommand, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(UpdateEmploymentStatusCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<EmploymentStatus>(cmd.Status, out var status))
        {
            return Error.Validation($"Status '{cmd.Status}' must be Active or OnLongTermAbsence.");
        }

        var employee = await db.Employees.FindAsync([cmd.EmployeeId], ct);
        if (employee is null)
        {
            return Error.NotFound("Employee not found.");
        }

        try { employee.SetEmploymentStatus(status); }
        catch (DomainStateException e) { return Error.Conflict(e.Message); }

        await db.SaveChangesAsync(ct);
        return (await EmployeeProjections.ByIdAsync(db, employee.Id, ct))!;
    }
}

internal static class UpdateEmploymentStatusEndpoint
{
    public static void MapUpdateEmploymentStatusEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/employment-status", async (Guid id, UpdateEmploymentStatusRequest body, UpdateEmploymentStatusHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateEmploymentStatusCommand(id, body.Status), ct)).ToHttp());
    }
}
