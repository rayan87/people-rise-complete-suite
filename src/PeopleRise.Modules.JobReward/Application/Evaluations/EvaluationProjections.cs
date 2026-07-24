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

        var maxPoints = await db.MethodologyVersions
            .Where(v => v.Id == eval.MethodologyVersionId)
            .Select(v => v.MaxPoints)
            .FirstAsync(ct);

        // Points can't be computed inside the query itself (Math.Round with MidpointRounding doesn't
        // reliably translate to SQL) - pull the raw weights/rating out, then compute in plain C#, same
        // approach as GetVersionDetail's CalculatedPoints.
        var answerRows = await (
            from a in db.EvaluationAnswers
            join q in db.Questions on a.QuestionId equals q.Id
            join o in db.AnswerOptions on a.AnswerOptionId equals o.Id
            join f in db.Factors on q.FactorId equals f.Id
            where a.EvaluationId == eval.Id
            orderby f.SortOrder, q.SortOrder
            select new
            {
                QuestionId = q.Id, q.QuestionTextEn, q.QuestionTextAr,
                AnswerOptionId = o.Id, o.LabelEn, o.LabelAr,
                a.RatingSnapshot,
                FactorId = f.Id, f.Code, f.NameEn, f.NameAr, FactorWeight = f.Weight,
                QuestionWeight = q.Weight,
            }).ToListAsync(ct);

        var answers = answerRows.Select(r =>
        {
            var questionPoints = maxPoints * r.FactorWeight / 100m * r.QuestionWeight / 100m;
            var points = (int)Math.Round(questionPoints * r.RatingSnapshot / 5m, MidpointRounding.AwayFromZero);
            return new AnswerAuditDto(
                r.QuestionId, r.QuestionTextEn, r.QuestionTextAr,
                r.AnswerOptionId, r.LabelEn, r.LabelAr, r.RatingSnapshot, points,
                r.FactorId, r.Code, r.NameEn, r.NameAr);
        }).ToList();

        return new EvaluationResultDto(
            eval.Id, eval.JobId, eval.Job?.Code ?? "", eval.Job?.TitleEn ?? "", eval.Job?.TitleAr,
            eval.MethodologyVersionId, eval.Status.ToString(),
            eval.TotalScore, eval.RecommendedGradeId,
            eval.RecommendedGrade?.Code, eval.RecommendedGrade?.NameEn, eval.RecommendedGrade?.NameAr,
            eval.SubmittedAt, eval.ApprovedAt, factorScores, answers);
    }
}
