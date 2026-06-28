using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

/// <summary>Builds the full evaluation result (header + factor breakdown + audit trail). Shared by
/// GetEvaluation, SubmitAnswers and ApproveEvaluation so the projection lives in one place.</summary>
internal static class EvaluationProjections
{
    public static async Task<EvaluationResultDto?> BuildAsync(JobRewardDbContext db, Guid id, CancellationToken ct)
    {
        var eval = await db.Evaluations
            .Include(e => e.Job)
            .Include(e => e.RecommendedGrade)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (eval is null) return null;

        var factorScores = await (
            from fs in db.EvaluationFactorScores
            join f in db.Factors on fs.FactorId equals f.Id
            where fs.EvaluationId == eval.Id
            orderby f.SortOrder
            select new FactorScoreDto(f.Id, f.Code, f.NameEn, f.NameAr, fs.Score)).ToListAsync(ct);

        var answers = await (
            from a in db.EvaluationAnswers
            join q in db.Questions on a.QuestionId equals q.Id
            join o in db.AnswerOptions on a.AnswerOptionId equals o.Id
            join f in db.Factors on q.FactorId equals f.Id
            where a.EvaluationId == eval.Id
            orderby f.SortOrder, q.SortOrder
            select new AnswerAuditDto(q.Id, q.QuestionTextEn, q.QuestionTextAr, o.Id, o.LabelEn, o.LabelAr, a.PointsSnapshot)).ToListAsync(ct);

        return new EvaluationResultDto(
            eval.Id, eval.JobId, eval.Job?.Code ?? "", eval.Job?.TitleEn ?? "", eval.Job?.TitleAr,
            eval.MethodologyVersionId, eval.Status.ToString(),
            eval.TotalScore, eval.RecommendedGradeId,
            eval.RecommendedGrade?.Code, eval.RecommendedGrade?.NameEn, eval.RecommendedGrade?.NameAr,
            eval.SubmittedAt, eval.ApprovedAt, factorScores, answers);
    }
}
