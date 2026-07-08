using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Application.Methodologies.Versions;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.ImportExport;

public sealed record ExportMethodologyVersionQuery(Guid VersionId);

internal sealed class ExportMethodologyVersionHandler(GetVersionDetailHandler getVersionDetail)
    : IQueryHandler<ExportMethodologyVersionQuery, Result<ExportedFile>>
{
    public async Task<Result<ExportedFile>> Handle(ExportMethodologyVersionQuery query, CancellationToken ct)
    {
        var detail = await getVersionDetail.Handle(new GetVersionDetailQuery(query.VersionId), ct);

        if (detail.IsFailure)
        {
            return detail.Error!;
        }

        var content = MethodologyWorkbook.Build(detail.Value);
        var fileName = $"{detail.Value.MethodologyCode}-v{detail.Value.VersionNo}.xlsx";

        return new ExportedFile(content, fileName);
    }
}

internal static class ExportMethodologyVersionEndpoint
{
    public static void MapExportMethodologyVersionEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/export", async (Guid id, ExportMethodologyVersionHandler h, CancellationToken ct) =>
        {
            var result = await h.Handle(new ExportMethodologyVersionQuery(id), ct);

            return result.IsSuccess
                ? Results.File(result.Value.Content, MethodologyWorkbook.ContentType, result.Value.FileName)
                : result.ToHttp();
        });
    }
}
