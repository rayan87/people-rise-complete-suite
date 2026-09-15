using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.PayElements;

public sealed record UpdatePayElementCommand(Guid Id, string Code, string NameEn, string? NameAr, PayBasis Basis);

internal sealed class UpdatePayElementHandler(CoreDbContext db)
    : ICommandHandler<UpdatePayElementCommand, Result<PayElementDto>>
{
    public async Task<Result<PayElementDto>> Handle(UpdatePayElementCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var element = await db.PayElements.FindAsync([cmd.Id], ct);
        if (element is null)
        {
            return Error.NotFound("Pay element not found.");
        }

        element.Update(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Basis);
        await db.SaveChangesAsync(ct);
        return new PayElementDto(element.Id, element.Code, element.NameEn, element.NameAr, element.Basis.ToString());
    }
}

internal static class UpdatePayElementEndpoint
{
    public static void MapUpdatePayElementEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdatePayElementCommand cmd, UpdatePayElementHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());
    }
}
