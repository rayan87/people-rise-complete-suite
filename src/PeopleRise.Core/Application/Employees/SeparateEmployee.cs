using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Employees;

// A separated employee is retained, not removed (Core Spec §8) - note there is no delete endpoint
// for Employee anywhere in this module.
public sealed record SeparateEmployeeCommand(Guid EmployeeId, DateOnly SeparationDate);

internal sealed class SeparateEmployeeHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<SeparateEmployeeCommand, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(SeparateEmployeeCommand cmd, CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync([cmd.EmployeeId], ct);
        if (employee is null)
        {
            return Error.NotFound("Employee not found.");
        }

        var currentAssignment = await db.EmployeeAssignments
            .FirstOrDefaultAsync(a => a.EmployeeId == cmd.EmployeeId && a.EndDate == null, ct);

        employee.Separate();

        Guid? vacatedPositionId = null;
        if (currentAssignment is not null)
        {
            currentAssignment.End(cmd.SeparationDate);
            var position = await db.JobPositions.FirstAsync(p => p.Id == currentAssignment.PositionId, ct);
            position.Vacate();
            vacatedPositionId = position.Id;
        }

        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new EmployeeSeparated(employee.Id), ct);
        if (vacatedPositionId is { } posId) await events.PublishAsync(new PositionVacated(posId), ct);
        return (await EmployeeProjections.ByIdAsync(db, employee.Id, ct))!;
    }
}

internal static class SeparateEmployeeEndpoint
{
    public static void MapSeparateEmployeeEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/separate", async (Guid id, SeparateEmployeeRequest body, SeparateEmployeeHandler h, CancellationToken ct) =>
            (await h.Handle(new SeparateEmployeeCommand(id, body.SeparationDate), ct)).ToHttp());
    }
}
