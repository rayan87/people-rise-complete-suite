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
        var versionDetail = await db.MethodologyVersions
            .Where(version => version.Id == query.VersionId)
            .Select(version => new MethodologyVersionDetailDto
            (
                version.Id,
                version.MethodologyId,
                version.Methodology!.Code,
                version.Methodology.NameEn,
                version.Methodology.NameAr,
                version.VersionNo,
                version.Status.ToString(),
                version.Note,
                version.PublishedAt,
                version.Factors!
                    .OrderBy(factor => factor.SortOrder)
                    .Select(factor => new FactorDetailDto
                    (
                        factor.Id,
                        factor.Code,
                        factor.NameEn,
                        factor.NameAr,
                        factor.Weight,
                        factor.SortOrder,
                        factor.Questions!
                            .OrderBy(question => question.SortOrder)
                            .Select(question => new QuestionDetailDto
                            (
                                question.Id,
                                question.QuestionTextEn,
                                question.QuestionTextAr,
                                question.HelpTextEn,
                                question.HelpTextAr,
                                question.QuestionType.ToString(),
                                question.SortOrder,
                                question.AnswerOptions!
                                    .OrderBy(option => option.SortOrder)
                                    .Select(option => new AnswerOptionDto
                                    (
                                        option.Id,
                                        option.LabelEn,
                                        option.LabelAr,
                                        option.Points,
                                        option.SortOrder
                                    ))
                                    .ToList()
                            ))
                            .ToList()
                    )).ToList(),
                version.GradeMappings!
                    .OrderBy(mapping => mapping.MinScore)
                    .Select(mapping => new GradeMappingDto
                    (
                        mapping.Id,
                        mapping.GradeId,
                        mapping.Grade!.Code,
                        mapping.MinScore,
                        mapping.MaxScore
                    ))
                    .ToList()
            )).FirstOrDefaultAsync(ct);

        if (versionDetail is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        return versionDetail;
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

