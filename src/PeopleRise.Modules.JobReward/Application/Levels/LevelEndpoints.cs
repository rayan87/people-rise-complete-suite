using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Levels;

internal static class LevelEndpoints
{
    public static void MapLevelEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/levels");

        g.MapGet("/", async (ListLevelsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListLevelsQuery(), ct)).ToHttp());

        g.MapPost("/", async (CreateLevelCommand cmd, CreateLevelHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());

        g.MapPut("/{id:guid}", async (Guid id, UpdateLevelCommand cmd, UpdateLevelHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());

        g.MapDelete("/{id:guid}", async (Guid id, DeleteLevelHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteLevelCommand(id), ct)).ToHttp());
    }
}
