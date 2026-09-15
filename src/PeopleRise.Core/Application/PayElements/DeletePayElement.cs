using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.PayElements;

public sealed record DeletePayElementCommand(Guid Id);

internal sealed class DeletePayElementHandler(CoreDbContext db)
    : ICommandHandler<DeletePayElementCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeletePayElementCommand cmd, CancellationToken ct)
    {
        var element = await db.PayElements.FindAsync(cmd.Id, ct);
        if (element is null)
        {
            return Error.NotFound("Pay element not found.");
        }

        var amountCount = await db.EmployeePayElementAmounts.CountAsync(a => a.PayElementId == cmd.Id, ct);
        if (amountCount > 0)
        {
            return Error.Conflict($"Pay element is in use by {amountCount} employee amount(s) — cannot delete.");
        }

        db.PayElements.Remove(element);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeletePayElementEndpoint
{
    public static void MapDeletePayElementEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeletePayElementHandler h, CancellationToken ct) =>
            (await h.Handle(new DeletePayElementCommand(id), ct)).ToHttp());
    }
}
