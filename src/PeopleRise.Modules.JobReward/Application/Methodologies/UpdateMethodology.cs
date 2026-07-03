using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record UpdateMethodologyCommand(Guid Id, string NameEn, string? NameAr);

internal sealed class UpdateMethodologyHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateMethodologyCommand, Result<MethodologyDto>>
{
    public async Task<Result<MethodologyDto>> Handle(UpdateMethodologyCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var methodology = await db.Methodologies
            .FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);

        if (methodology is null)
        {
            return Error.NotFound("Methodology not found.");
        }

        methodology.Update(cmd.NameEn, cmd.NameAr);
        await db.SaveChangesAsync(ct);
        return new MethodologyDto(methodology.Id, methodology.Code, methodology.NameEn, methodology.NameAr, []);
    }
}

internal static class UpdateMethodologyEndpoint
{
    public static void MapUpdateMethodologyEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}",
            async (Guid id, UpdateMethodologyRequest body, UpdateMethodologyHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateMethodologyCommand(id, body.NameEn, body.NameAr), ct)).ToHttp());
    }
}
