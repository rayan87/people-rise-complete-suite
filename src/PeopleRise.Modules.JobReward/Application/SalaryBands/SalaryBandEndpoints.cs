using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

internal static class SalaryBandEndpoints
{
    public static void MapSalaryBandEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/salary-bands");

        g.MapGet("/", async (ListSalaryBandsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListSalaryBandsQuery(), ct)).ToHttp());

        g.MapPost("/", async (CreateSalaryBandRequest body, CreateSalaryBandHandler h, CancellationToken ct) =>
            (await h.Handle(new CreateSalaryBandCommand(
                body.GradeId, body.Currency, body.Midpoint, body.SpreadPct, body.OverlapPct, body.EffectiveDate), ct)).ToHttp());

        g.MapPut("/{id:guid}", async (Guid id, UpdateSalaryBandRequest body, UpdateSalaryBandHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateSalaryBandCommand(
                id, body.Currency, body.Midpoint, body.SpreadPct, body.OverlapPct, body.EffectiveDate), ct)).ToHttp());

        g.MapPost("/generate", async (GenerateBandsRequest body, GenerateBandsHandler h, CancellationToken ct) =>
            (await h.Handle(new GenerateBandsCommand(
                body.BaseMidpoint, body.SpreadPct, body.ProgressionPct, body.Currency, body.EffectiveDate), ct)).ToHttp());
    }
}
