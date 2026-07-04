using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

internal static class EvaluationEndpoints
{
    public static void MapEvaluationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/evaluations");

        group.MapListEvaluationsEndpoint();
        group.MapCreateEvaluationEndpoint();
        group.MapSubmitAnswersEndpoint();
        group.MapGetEvaluationEndpoint();
        group.MapApproveEvaluationEndpoint();
        group.MapPost("/calibrate", async (CalibrateQuery query, CalibrateHandler h, CancellationToken ct) =>
            (await h.Handle(query, ct)).ToHttp());
    }
}
