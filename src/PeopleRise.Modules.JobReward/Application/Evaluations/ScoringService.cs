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
            questions.ToDictionary(q => q.Id, q => q.FactorId),
            questions.ToDictionary(q => q.Id, q => q));
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
    public static Result<ScoreComputation> Score(MethodologyStructure structure, IReadOnlyList<AnswerSelection> answers)
    {
        if (answers.Count == 0) return Error.Validation("No answers supplied.");

        var byQuestion = new Dictionary<Guid, AnswerSelection>();

        //Make sure no question answered more than once.
        foreach (var a in answers)
            if (!byQuestion.TryAdd(a.QuestionId, a))
                return Error.Validation($"Question {a.QuestionId} answered more than once.");

        //Make sure all questions have been answered.
        var missing = structure.Questions.Where(q => !byQuestion.ContainsKey(q.Id)).Select(q => q.Id).ToList();
        if (missing.Count > 0)
            return Error.Validation(
                $"{missing.Count} question(s) not answered; every question must be answered. First: {missing[0]}.");

        //Draw factor totals (factor structure empty table)
        var factorTotals = structure.Factors.ToDictionary(f => f.Id, _ => 0);

        //Prepare scored answers array (Answer score is a question with selected answer with collected points)
        var scoredAnswers = new List<AnswerScore>(answers.Count);

        //Loop through submitted answers
        foreach (var a in answers)
        {
            if (!structure.QuestionsById.TryGetValue(a.QuestionId, out var question))
                return Error.Validation($"Question {a.QuestionId} does not belong to this methodology version.");
            if (a.AnswerOptionIds.Count == 0)
                return Error.Validation($"Question {a.QuestionId} has no selected answer.");
            if (question.QuestionType == QuestionType.SingleChoice && a.AnswerOptionIds.Count > 1)
                return Error.Validation($"Question {a.QuestionId} allows only one selected answer.");
            if (a.AnswerOptionIds.Distinct().Count() != a.AnswerOptionIds.Count)
                return Error.Validation($"Question {a.QuestionId} has the same answer selected more than once.");

            //Sum selected answer options points into factor
            //Score each answer (fill answer score and factor totals table)
            foreach (var optionId in a.AnswerOptionIds)
            {
                if (!structure.OptionsById.TryGetValue(optionId, out var option))
                    return Error.Validation($"Answer option {optionId} not found.");
                if (option.QuestionId != a.QuestionId)
                    return Error.Validation($"Answer option {optionId} does not belong to question {a.QuestionId}.");

                scoredAnswers.Add(new AnswerScore(a.QuestionId, optionId, option.Points));
                factorTotals[structure.FactorByQuestion[a.QuestionId]] += option.Points;   // weight = 1.0 first cut
            }
        }

        var factorScores = structure.Factors.Select(f => (f.Id, factorTotals[f.Id])).ToList();
        var total = factorScores.Sum(fs => fs.Item2);
        return new ScoreComputation(total, factorScores, scoredAnswers);
    }
}

internal sealed record MethodologyStructure(
    IReadOnlyList<Factor> Factors,
    IReadOnlyList<Question> Questions,
    IReadOnlyDictionary<Guid, AnswerOption> OptionsById,
    IReadOnlyDictionary<Guid, Guid> FactorByQuestion,
    IReadOnlyDictionary<Guid, Question> QuestionsById);

internal readonly record struct AnswerScore(Guid QuestionId, Guid AnswerOptionId, int Points);

internal readonly record struct ScoreComputation(
    int Total,
    IReadOnlyList<(Guid FactorId, int Score)> FactorScores,
    IReadOnlyList<AnswerScore> Answers);
