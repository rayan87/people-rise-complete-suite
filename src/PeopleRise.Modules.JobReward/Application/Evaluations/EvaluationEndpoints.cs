using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

internal static class EvaluationEndpoints
{
    public static void MapEvaluationEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/evaluations");

        g.MapGet("/", async (ListEvaluationsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListEvaluationsQuery(), ct)).ToHttp());

        g.MapPost("/", async (CreateEvaluationCommand cmd, CreateEvaluationHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());

        g.MapPost("/{id:guid}/answers",
            async (Guid id, SubmitAnswersRequest body, SubmitAnswersHandler h, CancellationToken ct) =>
                (await h.Handle(new SubmitAnswersCommand(id, body.Answers ?? []), ct)).ToHttp());

        g.MapGet("/{id:guid}", async (Guid id, GetEvaluationHandler h, CancellationToken ct) =>
            (await h.Handle(new GetEvaluationQuery(id), ct)).ToHttp());

        g.MapPost("/{id:guid}/approve", async (Guid id, ApproveEvaluationHandler h, CancellationToken ct) =>
            (await h.Handle(new ApproveEvaluationCommand(id), ct)).ToHttp());

        g.MapPost("/calibrate", async (CalibrateQuery query, CalibrateHandler h, CancellationToken ct) =>
            (await h.Handle(query, ct)).ToHttp());
    }
}
