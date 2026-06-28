using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Grades;

internal static class GradeEndpoints
{
    public static void MapGradeEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/grades");

        g.MapGet("/", async (ListGradesHandler h, CancellationToken ct) =>
            (await h.Handle(new ListGradesQuery(), ct)).ToHttp());

        g.MapPost("/", async (CreateGradeCommand cmd, CreateGradeHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());

        g.MapPut("/{id:guid}", async (Guid id, UpdateGradeCommand cmd, UpdateGradeHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());

        g.MapDelete("/{id:guid}", async (Guid id, DeleteGradeHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteGradeCommand(id), ct)).ToHttp());
    }
}
