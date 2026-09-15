using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Employees;

// An employee always occupies a position - nobody is hired into no seat (Core Spec §8).
public sealed record HireEmployeeCommand(string EmployeeNo, string FullNameEn, string? FullNameAr, DateOnly HireDate, Guid PositionId);

internal sealed class HireEmployeeHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<HireEmployeeCommand, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(HireEmployeeCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.FullNameEn))
        {
            return Error.Validation("English full name is required.");
        }

        var position = await db.JobPositions.Include(p => p.OrgUnit)
            .FirstOrDefaultAsync(p => p.Id == cmd.PositionId, ct);
        if (position is null)
        {
            return Error.NotFound("Position not found.");
        }

        try { position.Occupy(); }
        catch (DomainStateException e) { return Error.Conflict(e.Message); }

        // Primary location is copied from the unit, not linked to it (Core Spec §8) - stored on the
        // roster and editable per employee from this point on.
        var employee = Employee.Create(cmd.EmployeeNo, cmd.FullNameEn, cmd.FullNameAr, cmd.HireDate, position.OrgUnit?.LocationId);
        db.Employees.Add(employee);
        db.EmployeeAssignments.Add(EmployeeAssignment.Create(employee.Id, position.Id, cmd.HireDate));

        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new EmployeeHired(employee.Id), ct);
        await events.PublishAsync(new PositionOccupied(position.Id, employee.Id), ct);
        return (await EmployeeProjections.ByIdAsync(db, employee.Id, ct))!;
    }
}

internal static class HireEmployeeEndpoint
{
    public static void MapHireEmployeeEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/hire", async (HireEmployeeRequest body, HireEmployeeHandler h, CancellationToken ct) =>
            (await h.Handle(new HireEmployeeCommand(body.EmployeeNo, body.FullNameEn, body.FullNameAr, body.HireDate, body.PositionId), ct)).ToHttp());
    }
}
