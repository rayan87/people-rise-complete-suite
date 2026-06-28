using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.JobFamilies;

internal static class JobFamilyEndpoints
{
    public static void MapJobFamilyEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/job-families");

        g.MapGet("/", async (ListJobFamiliesHandler h, CancellationToken ct) =>
            (await h.Handle(new ListJobFamiliesQuery(), ct)).ToHttp());

        g.MapPost("/", async (CreateJobFamilyCommand cmd, CreateJobFamilyHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());

        g.MapPut("/{id:guid}", async (Guid id, UpdateJobFamilyCommand cmd, UpdateJobFamilyHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());

        g.MapDelete("/{id:guid}", async (Guid id, DeleteJobFamilyHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteJobFamilyCommand(id), ct)).ToHttp());
    }
}
