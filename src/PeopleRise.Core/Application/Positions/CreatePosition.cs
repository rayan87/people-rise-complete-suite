using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Positions;

// Establishment is a declaration, not a workflow (Core Spec §7) - creating a position may require
// approval in the customer's own process; that belongs to Personnel, what reaches the core is the
// approved result.
public sealed record CreatePositionCommand(string Code, Guid JobId, Guid OrgUnitId);

internal sealed class CreatePositionHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<CreatePositionCommand, Result<PositionDto>>
{
    public async Task<Result<PositionDto>> Handle(CreatePositionCommand cmd, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == cmd.JobId, ct);
        if (job is null)
        {
            return Error.NotFound("Job not found.");
        }

        // Approved against a specific job WITHIN A GRADE (Core Spec §7) - not against a grade alone.
        // Grade is effective-dated (§3.2b/§6), so "graded" is a query, not a column.
        var isGraded = await db.JobGradeAssignments.AnyAsync(a => a.JobId == job.Id && a.EndDate == null, ct);
        if (!isGraded)
        {
            return Error.Validation("Job must be graded before a position can be approved for it.");
        }

        var orgUnit = await db.OrgUnits.FirstOrDefaultAsync(u => u.Id == cmd.OrgUnitId, ct);
        if (orgUnit is null)
        {
            return Error.NotFound("Org unit not found.");
        }
        if (orgUnit.Status == OrgUnitStatus.Closed)
        {
            return Error.Validation("Cannot approve a position against a closed org unit.");
        }

        var position = JobPosition.Create(cmd.Code, cmd.JobId, cmd.OrgUnitId);
        db.JobPositions.Add(position);
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new PositionApproved(position.Id), ct);
        return (await PositionProjections.ByIdAsync(db, position.Id, ct))!;
    }
}

internal static class CreatePositionEndpoint
{
    public static void MapCreatePositionEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreatePositionCommand cmd, CreatePositionHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}
