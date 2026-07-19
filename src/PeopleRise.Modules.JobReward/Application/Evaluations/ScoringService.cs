using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

/// <summary>
/// Server-side weighted point-factor scoring shared by SubmitAnswers and Calibrate (so the rules live once).
/// Factor points = MethodologyVersion.MaxPoints x Factor.Weight / 100. Question points = Factor points x
/// Question.Weight / 100. Question score = Question points x (rating / 5), rating being the unified 1-5
/// answer scale. An unanswered optional question's points are redistributed equally across the other
/// questions in the same factor.
/// </summary>
internal sealed class ScoringService(JobRewardDbContext db)
{
    public async Task<MethodologyStructure> LoadStructureAsync(Guid versionId, CancellationToken ct)
    {
        var version = await db.MethodologyVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == versionId, ct);
        var factors = await db.Factors.Where(f => f.MethodologyVersionId == versionId)
            .OrderBy(f => f.SortOrder).ToListAsync(ct);
        var factorIds = factors.Select(f => f.Id).ToList();
        var questions = await db.Questions.Where(q => factorIds.Contains(q.FactorId))
            .OrderBy(q => q.SortOrder).ToListAsync(ct);
        var questionIds = questions.Select(q => q.Id).ToList();
        var options = await db.AnswerOptions.Where(o => questionIds.Contains(o.QuestionId)).ToListAsync(ct);

        return new MethodologyStructure(
            version?.MinPoints ?? 0, version?.MaxPoints ?? 0,
            factors, questions,
            options.ToDictionary(o => o.Id, o => o),
            questions.ToDictionary(q => q.Id, q => q));
    }

    public async Task<Guid?> ResolveGradeIdAsync(Guid versionId, int total, CancellationToken ct)
    {
        var mapping = await db.GradeMappings
            .Where(m => m.MethodologyVersionId == versionId
                     && m.MinScore != null && m.MaxScore != null
                     && m.MinScore <= total && total <= m.MaxScore)
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

        //Only required questions must be answered - optional questions may be skipped (their points
        //are redistributed to the rest of their factor below).
        var missingRequired = structure.Questions
            .Where(q => q.IsRequired && !byQuestion.ContainsKey(q.Id))
            .Select(q => q.Id).ToList();
        if (missingRequired.Count > 0)
            return Error.Validation(
                $"{missingRequired.Count} required question(s) not answered. First: {missingRequired[0]}.");

        //Weighted points: methodology budget -> factor points -> question points.
        var questionPoints = new Dictionary<Guid, decimal>();
        foreach (var factor in structure.Factors)
        {
            var factorPoints = structure.MaxPoints * factor.Weight / 100m;
            foreach (var q in structure.Questions.Where(q => q.FactorId == factor.Id))
                questionPoints[q.Id] = factorPoints * q.Weight / 100m;
        }

        //Redistribute unanswered optional questions' points equally across the other questions in
        //the same factor, so skipping one never shrinks the factor's achievable total.
        var effectivePoints = new Dictionary<Guid, decimal>(questionPoints);
        foreach (var factor in structure.Factors)
        {
            var factorQuestions = structure.Questions.Where(q => q.FactorId == factor.Id).ToList();
            foreach (var unanswered in factorQuestions.Where(q => !q.IsRequired && !byQuestion.ContainsKey(q.Id)))
            {
                var siblings = factorQuestions.Where(q => q.Id != unanswered.Id).ToList();
                if (siblings.Count == 0) continue;   // sole question in its factor, skipped - nothing to redistribute to

                var share = questionPoints[unanswered.Id] / siblings.Count;
                foreach (var sibling in siblings)
                    effectivePoints[sibling.Id] += share;
            }
        }

        //Draw factor totals (factor structure empty table)
        var factorTotals = structure.Factors.ToDictionary(f => f.Id, _ => 0);

        //Prepare scored answers array (Answer score is a question with selected answer with its rating)
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

            var ratings = new List<int>(a.AnswerOptionIds.Count);
            foreach (var optionId in a.AnswerOptionIds)
            {
                if (!structure.OptionsById.TryGetValue(optionId, out var option))
                    return Error.Validation($"Answer option {optionId} not found.");
                if (option.QuestionId != a.QuestionId)
                    return Error.Validation($"Answer option {optionId} does not belong to question {a.QuestionId}.");

                ratings.Add(option.Rating);
                scoredAnswers.Add(new AnswerScore(a.QuestionId, optionId, option.Rating));
            }

            // Unified 1-5 rating: single choice uses that rating directly; multiple choice averages
            // the selected ratings so the question score never exceeds its allocated points.
            var effectiveRating = (decimal)ratings.Sum() / ratings.Count;
            var questionScore = effectivePoints[a.QuestionId] * (effectiveRating / 5m);
            factorTotals[question.FactorId] += (int)Math.Round(questionScore, MidpointRounding.AwayFromZero);
        }

        var factorScores = structure.Factors.Select(f => (f.Id, factorTotals[f.Id])).ToList();
        var total = factorScores.Sum(fs => fs.Item2);
        return new ScoreComputation(total, factorScores, scoredAnswers);
    }
}

internal sealed record MethodologyStructure(
    int MinPoints,
    int MaxPoints,
    IReadOnlyList<Factor> Factors,
    IReadOnlyList<Question> Questions,
    IReadOnlyDictionary<Guid, AnswerOption> OptionsById,
    IReadOnlyDictionary<Guid, Question> QuestionsById);

internal readonly record struct AnswerScore(Guid QuestionId, Guid AnswerOptionId, int Rating);

internal readonly record struct ScoreComputation(
    int Total,
    IReadOnlyList<(Guid FactorId, int Score)> FactorScores,
    IReadOnlyList<AnswerScore> Answers);
