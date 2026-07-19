using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Factors;

public sealed record UpdateFactorCommand(Guid VersionId, Guid FactorId, string Code, string NameEn, string? NameAr, string? HelpTextEn, string? HelpTextAr, int SortOrder, decimal Weight);

internal sealed class UpdateFactorHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateFactorCommand, Result<FactorDto>>
{
    public async Task<Result<FactorDto>> Handle(UpdateFactorCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var version = await db.MethodologyVersions
            .Include(v => v.Factors)
            .Where(v => v.Id == cmd.VersionId)
            .FirstOrDefaultAsync(ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        Factor? factor;

        try 
        {
            factor = version.UpdateFactor(cmd.FactorId,
                cmd.Code,
                cmd.NameEn,
                cmd.NameAr,
                cmd.HelpTextEn,
                cmd.HelpTextAr,
                cmd.Weight,
                cmd.SortOrder);

            if (factor is null)
            {
                return Error.NotFound("Factor not found.");
            }
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
        return new FactorDto(factor.Id, factor.Code, factor.NameEn, factor.NameAr, factor.HelpTextEn, factor.HelpTextAr, factor.Weight, factor.SortOrder);
    }
}

internal static class UpdateFactorEndpoint
{
    public static void MapUpdateFactorEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{versionId:guid}/factors/{factorId:guid}",
            async (Guid versionId, Guid factorId, FactorRequest body, UpdateFactorHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateFactorCommand(versionId, factorId, body.Code, body.NameEn, body.NameAr, body.HelpTextEn, body.HelpTextAr, body.SortOrder, body.Weight), ct)).ToHttp());
    }
}
