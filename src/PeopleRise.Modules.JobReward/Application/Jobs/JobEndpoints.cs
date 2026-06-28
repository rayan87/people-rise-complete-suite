using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Jobs;

internal static class JobEndpoints
{
    public static void MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/jobs");

        g.MapGet("/", async (ListJobsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListJobsQuery(), ct)).ToHttp());

        g.MapGet("/{id:guid}", async (Guid id, GetJobHandler h, CancellationToken ct) =>
            (await h.Handle(new GetJobQuery(id), ct)).ToHttp());

        g.MapPost("/", async (CreateJobCommand cmd, CreateJobHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());

        g.MapPut("/{id:guid}", async (Guid id, UpdateJobCommand cmd, UpdateJobHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());

        g.MapDelete("/{id:guid}", async (Guid id, DeleteJobHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteJobCommand(id), ct)).ToHttp());
    }
}
