using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

/// <summary>
/// Server-side point-factor scoring shared by SubmitAnswers and Calibrate (so the rules live once).
/// First cut: per-factor subtotal = plain sum of selected answer points (factor weight = 1.0).
/// </summary>
internal sealed class ScoringService(JobRewardDbContext db)
{
    public async Task<MethodologyStructure> LoadStructureAsync(Guid versionId, CancellationToken ct)
    {
        var factors = await db.Factors.Where(f => f.MethodologyVersionId == versionId)
            .OrderBy(f => f.SortOrder).ToListAsync(ct);
        var factorIds = factors.Select(f => f.Id).ToList();
        var questions = await db.Questions.Where(q => factorIds.Contains(q.FactorId))
            .OrderBy(q => q.SortOrder).ToListAsync(ct);
        var questionIds = questions.Select(q => q.Id).ToList();
        var options = await db.AnswerOptions.Where(o => questionIds.Contains(o.QuestionId)).ToListAsync(ct);

        return new MethodologyStructure(
            factors, questions,
            options.ToDictionary(o => o.Id, o => o),
            questions.ToDictionary(q => q.Id, q => q.FactorId));
    }

    public async Task<Guid?> ResolveGradeIdAsync(Guid versionId, int total, CancellationToken ct)
    {
        var mapping = await db.GradeMappings
            .Where(m => m.MethodologyVersionId == versionId && m.MinScore <= total && total <= m.MaxScore)
            .OrderBy(m => m.MinScore)
            .FirstOrDefaultAsync(ct);
        return mapping?.GradeId;
    }

    /// <summary>Pure validation + scoring against a loaded structure. No DB, no persistence.</summary>
    public static Result<ScoreComputation> Score(MethodologyStructure s, IReadOnlyList<AnswerSelection> answers)
    {
        if (answers.Count == 0) return Error.Validation("No answers supplied.");

        var byQuestion = new Dictionary<Guid, AnswerSelection>();
        foreach (var a in answers)
            if (!byQuestion.TryAdd(a.QuestionId, a))
                return Error.Validation($"Question {a.QuestionId} answered more than once.");

        var missing = s.Questions.Where(q => !byQuestion.ContainsKey(q.Id)).Select(q => q.Id).ToList();
        if (missing.Count > 0)
            return Error.Validation(
                $"{missing.Count} question(s) not answered; every question must be answered. First: {missing[0]}.");

        var validQuestionIds = s.Questions.Select(q => q.Id).ToHashSet();
        var factorTotals = s.Factors.ToDictionary(f => f.Id, _ => 0);
        var scoredAnswers = new List<AnswerScore>(answers.Count);

        foreach (var a in answers)
        {
            if (!validQuestionIds.Contains(a.QuestionId))
                return Error.Validation($"Question {a.QuestionId} does not belong to this methodology version.");
            if (!s.OptionsById.TryGetValue(a.AnswerOptionId, out var option))
                return Error.Validation($"Answer option {a.AnswerOptionId} not found.");
            if (option.QuestionId != a.QuestionId)
                return Error.Validation($"Answer option {a.AnswerOptionId} does not belong to question {a.QuestionId}.");

            scoredAnswers.Add(new AnswerScore(a.QuestionId, a.AnswerOptionId, option.Points));
            factorTotals[s.FactorByQuestion[a.QuestionId]] += option.Points;   // weight = 1.0 first cut
        }

        var factorScores = s.Factors.Select(f => (f.Id, factorTotals[f.Id])).ToList();
        var total = factorScores.Sum(fs => fs.Item2);
        return new ScoreComputation(total, factorScores, scoredAnswers);
    }
}

internal sealed record MethodologyStructure(
    IReadOnlyList<Factor> Factors,
    IReadOnlyList<Question> Questions,
    IReadOnlyDictionary<Guid, AnswerOption> OptionsById,
    IReadOnlyDictionary<Guid, Guid> FactorByQuestion);

internal readonly record struct AnswerScore(Guid QuestionId, Guid AnswerOptionId, int Points);

internal readonly record struct ScoreComputation(
    int Total,
    IReadOnlyList<(Guid FactorId, int Score)> FactorScores,
    IReadOnlyList<AnswerScore> Answers);
