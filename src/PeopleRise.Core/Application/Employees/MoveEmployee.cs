using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Employees;

// A person between seats is moved, not unassigned (Core Spec §8) - there is no bare "vacate".
public sealed record MoveEmployeeCommand(Guid EmployeeId, Guid NewPositionId, DateOnly EffectiveDate);

internal sealed class MoveEmployeeHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<MoveEmployeeCommand, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(MoveEmployeeCommand cmd, CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync([cmd.EmployeeId], ct);
        if (employee is null)
        {
            return Error.NotFound("Employee not found.");
        }

        var currentAssignment = await db.EmployeeAssignments
            .FirstOrDefaultAsync(a => a.EmployeeId == cmd.EmployeeId && a.EndDate == null, ct);
        if (currentAssignment is null)
        {
            return Error.Conflict("Employee has no current assignment to move from.");
        }

        var newPosition = await db.JobPositions.Include(p => p.OrgUnit)
            .FirstOrDefaultAsync(p => p.Id == cmd.NewPositionId, ct);
        if (newPosition is null)
        {
            return Error.NotFound("New position not found.");
        }

        try { newPosition.Occupy(); }
        catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var oldPositionId = currentAssignment.PositionId;
        var oldPosition = await db.JobPositions.FirstAsync(p => p.Id == oldPositionId, ct);

        currentAssignment.End(cmd.EffectiveDate);
        oldPosition.Vacate();
        db.EmployeeAssignments.Add(EmployeeAssignment.Create(cmd.EmployeeId, cmd.NewPositionId, cmd.EffectiveDate));
        employee.SetPrimaryLocation(newPosition.OrgUnit?.LocationId);

        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new PositionVacated(oldPositionId), ct);
        await events.PublishAsync(new PositionOccupied(cmd.NewPositionId, employee.Id), ct);
        await events.PublishAsync(new EmployeeAssignmentChanged(employee.Id, cmd.NewPositionId), ct);
        return (await EmployeeProjections.ByIdAsync(db, employee.Id, ct))!;
    }
}

internal static class MoveEmployeeEndpoint
{
    public static void MapMoveEmployeeEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/move", async (Guid id, MoveEmployeeRequest body, MoveEmployeeHandler h, CancellationToken ct) =>
            (await h.Handle(new MoveEmployeeCommand(id, body.NewPositionId, body.EffectiveDate), ct)).ToHttp());
    }
}
