using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Application.Methodologies.Versions;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.ImportExport;

public sealed record ImportMethodologyVersionCommand(Guid MethodologyId, string? Note, byte[] FileContent);

// Import always creates a NEW Draft version - it never edits an existing one.
// Publishing (making it the one evaluations are scored against) stays a separate, explicit step.
internal sealed class ImportMethodologyVersionHandler(JobRewardDbContext db, GetVersionDetailHandler getVersionDetail)
    : ICommandHandler<ImportMethodologyVersionCommand, Result<MethodologyVersionDetailDto>>
{
    public async Task<Result<MethodologyVersionDetailDto>> Handle(ImportMethodologyVersionCommand cmd, CancellationToken ct)
    {
        if (!await db.Methodologies.AnyAsync(m => m.Id == cmd.MethodologyId, ct))
        {
            return Error.NotFound("Methodology not found.");
        }

        var parsed = MethodologyWorkbook.Parse(cmd.FileContent);

        if (parsed.IsFailure)
        {
            return parsed.Error!;
        }

        var workbook = parsed.Value;

        var gradeCodes = workbook.GradeMappings.Select(g => g.GradeCode).Distinct().ToList();
        var gradeIdsByCode = await db.Grades
            .Where(g => gradeCodes.Contains(g.Code))
            .ToDictionaryAsync(g => g.Code, g => g.Id, ct);

        var missingGradeCodes = gradeCodes.Where(c => !gradeIdsByCode.ContainsKey(c)).ToList();

        if (missingGradeCodes.Count > 0)
        {
            return Error.Validation($"Unknown grade code(s): {string.Join(", ", missingGradeCodes)}.");
        }

        var nextVersionNo = await db.MethodologyVersions
            .Where(v => v.MethodologyId == cmd.MethodologyId)
            .Select(v => (int?)v.VersionNo).MaxAsync(ct) ?? 0;

        MethodologyVersion version;

        try
        {
            version = MethodologyVersion.CreateDraft(cmd.MethodologyId, nextVersionNo + 1, cmd.Note, workbook.VersionInfo.MinPoints, workbook.VersionInfo.MaxPoints);
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        db.MethodologyVersions.Add(version);

        var factorsByKey = new Dictionary<int, Factor>();
        var questionsByKey = new Dictionary<int, Question>();

        try
        {
            foreach (var row in workbook.Factors)
            {
                factorsByKey[row.Key] = version.AddFactor(row.Code, row.NameEn, row.NameAr, null, null, row.Weight, row.SortOrder);
            }

            foreach (var row in workbook.Questions)
            {
                if (!Enum.TryParse<QuestionType>(row.QuestionType, out var questionType))
                {
                    return Error.Validation($"Questions: QuestionType '{row.QuestionType}' must be SingleChoice or MultipleChoice.");
                }

                var factor = factorsByKey[row.FactorKey];
                questionsByKey[row.Key] = factor.AddQuestion(row.QuestionTextEn, row.QuestionTextAr, row.HelpTextEn, row.HelpTextAr, questionType, row.Weight, row.IsRequired, row.SortOrder);
            }

            foreach (var row in workbook.AnswerOptions)
            {
                var question = questionsByKey[row.QuestionKey];
                question.AddAnswerOption(row.LabelEn, row.LabelAr, null, null, row.Rating, row.SortOrder);
            }

            foreach (var row in workbook.GradeMappings)
            {
                version.AddGradeMapping(gradeIdsByCode[row.GradeCode], row.MinScore, row.MaxScore);
            }
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        await db.SaveChangesAsync(ct);

        return await getVersionDetail.Handle(new GetVersionDetailQuery(version.Id), ct);
    }
}

internal static class ImportMethodologyVersionEndpoint
{
    public static void MapImportMethodologyVersionEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/versions/import",
            async (Guid id, IFormFile file, [FromForm] string? note, ImportMethodologyVersionHandler h, CancellationToken ct) =>
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream, ct);

                return (await h.Handle(new ImportMethodologyVersionCommand(id, note, stream.ToArray()), ct)).ToHttp();
            })
            .DisableAntiforgery();
    }
}
