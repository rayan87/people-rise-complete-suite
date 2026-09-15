using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.PayElements;

public sealed record CreatePayElementCommand(string Code, string NameEn, string? NameAr, PayBasis Basis);

internal sealed class CreatePayElementHandler(CoreDbContext db)
    : ICommandHandler<CreatePayElementCommand, Result<PayElementDto>>
{
    public async Task<Result<PayElementDto>> Handle(CreatePayElementCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var element = PayElement.Create(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Basis);
        db.PayElements.Add(element);
        await db.SaveChangesAsync(ct);
        return new PayElementDto(element.Id, element.Code, element.NameEn, element.NameAr, element.Basis.ToString());
    }
}

internal static class CreatePayElementEndpoint
{
    public static void MapCreatePayElementEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreatePayElementCommand cmd, CreatePayElementHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}
