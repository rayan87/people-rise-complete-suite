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
