using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.JobFamilies;

public sealed record DeleteJobFamilyCommand(Guid Id);

// Closed, never deleted, once anything has referenced it (Core Spec §3.4). Jobs and salary bands
// are never hard-deleted once referenced either, so checking current rows already covers full
// history.
internal sealed class DeleteJobFamilyHandler(CoreDbContext db)
    : ICommandHandler<DeleteJobFamilyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteJobFamilyCommand cmd, CancellationToken ct)
    {
        var family = await db.JobFamilies.FindAsync(cmd.Id, ct);

        if (family is null)
        {
            return Error.NotFound("Job family not found.");
        }

        var everReferenced = await db.Jobs.AnyAsync(j => j.JobFamilyId == cmd.Id, ct)
            || await db.SalaryBands.AnyAsync(b => b.JobFamilyId == cmd.Id, ct);

        if (everReferenced)
        {
            family.Close();
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        db.JobFamilies.Remove(family);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteJobFamilyEndpoint
{
    public static void MapDeleteJobFamilyEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteJobFamilyHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteJobFamilyCommand(id), ct)).ToHttp());
    }
}
