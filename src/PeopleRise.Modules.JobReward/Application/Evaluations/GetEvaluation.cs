using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public sealed record GetEvaluationQuery(Guid Id);

internal sealed class GetEvaluationHandler(JobRewardDbContext db)
    : IQueryHandler<GetEvaluationQuery, Result<EvaluationResultDto>>
{
    public async Task<Result<EvaluationResultDto>> Handle(GetEvaluationQuery query, CancellationToken ct)
    {
        var result = await EvaluationProjections.BuildAsync(db, query.Id, ct);
        return result is null ? Error.NotFound("Evaluation not found.") : result;
    }
}

internal static class GetEvaluationEndpoint
{
    public static void MapGetEvaluationEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, GetEvaluationHandler h, CancellationToken ct) =>
            (await h.Handle(new GetEvaluationQuery(id), ct)).ToHttp());
    }
}

