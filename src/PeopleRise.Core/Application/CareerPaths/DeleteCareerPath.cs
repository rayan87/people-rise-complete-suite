using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.CareerPaths;

public sealed record DeleteCareerPathCommand(Guid Id);

// Nothing outside a CareerPath ever references it (a step is qualification-adjacent, not a fact
// another entity points at - Talent Management, not built, would be the only consumer per §5), so
// it's always a plain delete, no closure needed (Core Spec §3.4: deletion stays available for
// records nothing references).
internal sealed class DeleteCareerPathHandler(CoreDbContext db)
    : ICommandHandler<DeleteCareerPathCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteCareerPathCommand cmd, CancellationToken ct)
    {
        var path = await db.CareerPaths.FindAsync(cmd.Id, ct);
        if (path is null) return Error.NotFound("Career path not found.");

        var steps = await db.CareerPathSteps.Where(s => s.CareerPathId == cmd.Id).ToListAsync(ct);
        db.CareerPathSteps.RemoveRange(steps);
        db.CareerPaths.Remove(path);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteCareerPathEndpoint
{
    public static void MapDeleteCareerPathEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteCareerPathHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteCareerPathCommand(id), ct)).ToHttp());
    }
}
