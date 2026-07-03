using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record CreateMethodologyCommand(string Code, string NameEn, string? NameAr);

internal sealed class CreateMethodologyHandler(JobRewardDbContext db)
    : ICommandHandler<CreateMethodologyCommand, Result<MethodologyDto>>
{
    public async Task<Result<MethodologyDto>> Handle(CreateMethodologyCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        var m = Methodology.Create(cmd.Code, cmd.NameEn, cmd.NameAr);
        db.Methodologies.Add(m);
        await db.SaveChangesAsync(ct);
        return new MethodologyDto(m.Id, m.Code, m.NameEn, m.NameAr, []);
    }
}

internal static class CreateMethodologyEndpoint
{
    public static void MapCreateMethodologyEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateMethodologyCommand cmd, CreateMethodologyHandler h, CancellationToken ct) =>
           (await h.Handle(cmd, ct)).ToHttp());
    }
}
