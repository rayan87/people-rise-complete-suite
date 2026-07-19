using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Versions;

public sealed record SetPointBudgetCommand(Guid VersionId, int MinPoints, int MaxPoints);

internal sealed class SetPointBudgetHandler(JobRewardDbContext db)
    : ICommandHandler<SetPointBudgetCommand, Result<MethodologyVersionDto>>
{
    public async Task<Result<MethodologyVersionDto>> Handle(SetPointBudgetCommand cmd, CancellationToken ct)
    {
        var version = await db.MethodologyVersions.FirstOrDefaultAsync(v => v.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        try
        {
            version.SetPointBudget(cmd.MinPoints, cmd.MaxPoints);
        }
        catch (DomainStateException e)
        {
            return Error.Conflict(e.Message);
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        await db.SaveChangesAsync(ct);
        return new MethodologyVersionDto(version.Id, version.VersionNo, version.Status.ToString(), version.Note, version.MinPoints, version.MaxPoints, version.PublishedAt);
    }
}

internal static class SetPointBudgetEndpoint
{
    public static void MapSetPointBudgetEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/point-budget",
            async (Guid id, SetPointBudgetRequest body, SetPointBudgetHandler h, CancellationToken ct) =>
                (await h.Handle(new SetPointBudgetCommand(id, body.MinPoints, body.MaxPoints), ct)).ToHttp());
    }
}
