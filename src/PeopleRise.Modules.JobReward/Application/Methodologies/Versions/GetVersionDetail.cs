using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Versions;

public sealed record GetVersionDetailQuery(Guid VersionId);

// Serves BOTH the authoring screen and the evaluation form (the questionnaire).
internal sealed class GetVersionDetailHandler(JobRewardDbContext db)
    : IQueryHandler<GetVersionDetailQuery, Result<MethodologyVersionDetailDto>>
{
    public async Task<Result<MethodologyVersionDetailDto>> Handle(GetVersionDetailQuery query, CancellationToken ct)
    {
        var version = await db.MethodologyVersions
            .Include(v => v.Methodology)
            .Include(v => v.Factors!).ThenInclude(f => f.Questions!).ThenInclude(q => q.AnswerOptions)
            .Include(v => v.GradeMappings!).ThenInclude(g => g.Grade)
            .FirstOrDefaultAsync(v => v.Id == query.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        // Calculated points are built in plain C# (not the LINQ-to-Entities query above) so the same
        // weighted-points formula ScoringService uses is free to call Math.Round with
        // MidpointRounding, which isn't guaranteed to translate to SQL.
        var factorDtos = version.Factors!
            .OrderBy(factor => factor.SortOrder)
            .Select(factor =>
            {
                var factorPoints = version.MaxPoints * factor.Weight / 100m;

                var questionDtos = factor.Questions!
                    .OrderBy(question => question.SortOrder)
                    .Select(question =>
                    {
                        var questionPoints = factorPoints * question.Weight / 100m;

                        var optionDtos = question.AnswerOptions!
                            .OrderBy(option => option.SortOrder)
                            .Select(option => new AnswerOptionDto(
                                option.Id,
                                option.LabelEn,
                                option.LabelAr,
                                option.HelpTextEn,
                                option.HelpTextAr,
                                option.Rating,
                                option.SortOrder,
                                (int)Math.Round(questionPoints * option.Rating / 5m, MidpointRounding.AwayFromZero)))
                            .ToList();

                        return new QuestionDetailDto(
                            question.Id,
                            question.QuestionTextEn,
                            question.QuestionTextAr,
                            question.HelpTextEn,
                            question.HelpTextAr,
                            question.QuestionType.ToString(),
                            question.Weight,
                            question.IsRequired,
                            question.SortOrder,
                            questionPoints,
                            optionDtos);
                    })
                    .ToList();

                return new FactorDetailDto(
                    factor.Id,
                    factor.Code,
                    factor.NameEn,
                    factor.NameAr,
                    factor.HelpTextEn,
                    factor.HelpTextAr,
                    factor.Weight,
                    factor.SortOrder,
                    factorPoints,
                    questionDtos);
            })
            .ToList();

        var gradeMappingDtos = version.GradeMappings!
            .OrderBy(mapping => mapping.MinScore)
            .Select(mapping => new GradeMappingDto(
                mapping.Id,
                mapping.GradeId,
                mapping.Grade!.Code,
                mapping.MinScore,
                mapping.MaxScore))
            .ToList();

        return new MethodologyVersionDetailDto(
            version.Id,
            version.MethodologyId,
            version.Methodology!.Code,
            version.Methodology.NameEn,
            version.Methodology.NameAr,
            version.VersionNo,
            version.Status.ToString(),
            version.Note,
            version.MinPoints,
            version.MaxPoints,
            version.PublishedAt,
            factorDtos,
            gradeMappingDtos);
    }
}

internal static class GetVersionDetailEndpoint
{
    public static void MapGetVersionDetailEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, GetVersionDetailHandler h, CancellationToken ct) =>
            (await h.Handle(new GetVersionDetailQuery(id), ct)).ToHttp());
    }
}
