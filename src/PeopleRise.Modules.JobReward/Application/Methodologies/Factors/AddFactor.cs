using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Factors;

public sealed record AddFactorCommand(Guid VersionId, string Code, string NameEn, string? NameAr, string? HelpTextEn, string? HelpTextAr, int SortOrder, decimal Weight);

internal sealed class AddFactorHandler(JobRewardDbContext db)
    : ICommandHandler<AddFactorCommand, Result<FactorDto>>
{
    public async Task<Result<FactorDto>> Handle(AddFactorCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var version = await db.MethodologyVersions
            .FirstOrDefaultAsync(v => v.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        Factor? factor;

        try
        {
            factor = version.AddFactor(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.HelpTextEn, cmd.HelpTextAr, cmd.Weight, cmd.SortOrder);
        }
        catch (DomainStateException e) 
        { 
            return Error.Conflict(e.Message); 
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        db.Factors.Add(factor);
        await db.SaveChangesAsync(ct);

        return new FactorDto(factor.Id, factor.Code, factor.NameEn, factor.NameAr, factor.HelpTextEn, factor.HelpTextAr, factor.Weight, factor.SortOrder);
    }
}

internal static class AddFactorEndpoint
{
    public static void MapAddFactorEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/factors",
            async (Guid id, FactorRequest body, AddFactorHandler h, CancellationToken ct) =>
                (await h.Handle(new AddFactorCommand(id, body.Code, body.NameEn, body.NameAr, body.HelpTextEn, body.HelpTextAr, body.SortOrder, body.Weight), ct)).ToHttp());

    }
}
