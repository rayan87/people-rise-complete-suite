using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record GetVersionDetailQuery(Guid VersionId);

// Serves BOTH the authoring screen and the evaluation form (the questionnaire).
internal sealed class GetVersionDetailHandler(JobRewardDbContext db)
    : IQueryHandler<GetVersionDetailQuery, Result<MethodologyVersionDetailDto>>
{
    public async Task<Result<MethodologyVersionDetailDto>> Handle(GetVersionDetailQuery query, CancellationToken ct)
    {
        var id = query.VersionId;
        var v = await db.MethodologyVersions.Include(x => x.Methodology).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return Error.NotFound("Methodology version not found.");

        var factors = await db.Factors.Where(f => f.MethodologyVersionId == id).OrderBy(f => f.SortOrder)
            .Select(f => new FactorDetailDto(
                f.Id, f.Code, f.NameEn, f.NameAr, f.Weight, f.SortOrder,
                db.Questions.Where(q => q.FactorId == f.Id).OrderBy(q => q.SortOrder)
                    .Select(q => new QuestionDetailDto(
                        q.Id, q.QuestionTextEn, q.QuestionTextAr, q.HelpTextEn, q.HelpTextAr, q.QuestionType.ToString(), q.SortOrder,
                        db.AnswerOptions.Where(o => o.QuestionId == q.Id).OrderBy(o => o.SortOrder)
                            .Select(o => new AnswerOptionDto(o.Id, o.LabelEn, o.LabelAr, o.Points, o.SortOrder)).ToList()))
                    .ToList())).ToListAsync(ct);

        var mappings = await db.GradeMappings.Where(gm => gm.MethodologyVersionId == id)
            .OrderBy(gm => gm.MinScore)
            .Select(gm => new GradeMappingDto(gm.Id, gm.GradeId, gm.Grade!.Code, gm.MinScore, gm.MaxScore))
            .ToListAsync(ct);

        return new MethodologyVersionDetailDto(
            v.Id, v.MethodologyId, v.Methodology!.Code, v.Methodology.NameEn, v.Methodology.NameAr,
            v.VersionNo, v.Status.ToString(), v.Note, v.PublishedAt, factors, mappings);
    }
}
