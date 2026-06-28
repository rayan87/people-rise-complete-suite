using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

/// <summary>The calibration gate: dry-run score a set of known jobs and rank them — no persistence.</summary>
public sealed record CalibrateQuery(Guid MethodologyVersionId, IReadOnlyList<CalibrationJob> Jobs);

internal sealed class CalibrateHandler(JobRewardDbContext db, ScoringService scoring)
    : IQueryHandler<CalibrateQuery, Result<CalibrationResultDto>>
{
    public async Task<Result<CalibrationResultDto>> Handle(CalibrateQuery query, CancellationToken ct)
    {
        if (!await db.MethodologyVersions.AnyAsync(v => v.Id == query.MethodologyVersionId, ct))
            return Error.NotFound("Methodology version not found.");

        var structure = await scoring.LoadStructureAsync(query.MethodologyVersionId, ct);
        if (structure.Questions.Count == 0)
            return Error.Validation("The methodology version has no questions to score.");

        var jobIds = query.Jobs.Select(j => j.JobId).ToList();
        var jobLookup = await db.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, ct);
        var grades = await db.Grades.ToDictionaryAsync(g => g.Id, ct);

        var scored = new List<(Guid JobId, int Total, Guid? GradeId)>();
        foreach (var jb in query.Jobs)
        {
            if (!jobLookup.ContainsKey(jb.JobId)) return Error.NotFound($"Job {jb.JobId} not found.");
            var computed = ScoringService.Score(structure, jb.Answers);
            if (computed.IsFailure) return Error.Validation($"Job {jb.JobId}: {computed.Error!.Message}");
            var gradeId = await scoring.ResolveGradeIdAsync(query.MethodologyVersionId, computed.Value.Total, ct);
            scored.Add((jb.JobId, computed.Value.Total, gradeId));
        }

        var ranking = scored
            .OrderByDescending(s => s.Total)
            .Select((s, i) =>
            {
                var job = jobLookup[s.JobId];
                var gradeCode = s.GradeId is { } gid && grades.TryGetValue(gid, out var g) ? g.Code : null;
                return new CalibrationRowDto(i + 1, s.JobId, job.Code, job.TitleEn, job.TitleAr, s.Total, s.GradeId, gradeCode);
            })
            .ToList();

        return new CalibrationResultDto(query.MethodologyVersionId, ranking);
    }
}
