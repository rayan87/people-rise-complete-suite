using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.Competencies;

internal static class CompetencyEndpoints
{
    public static void MapCompetencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/competencies");
        group.MapListCompetenciesEndpoint();
        group.MapCreateCompetencyEndpoint();
        group.MapUpdateCompetencyEndpoint();
        group.MapDeleteCompetencyEndpoint();

        var templates = app.MapGroup("/competency-templates");
        templates.MapListTemplatesEndpoint();
        templates.MapCreateTemplateEndpoint();
        templates.MapSetTemplateItemsEndpoint();
        templates.MapDeleteTemplateEndpoint();

        // Required profile, held profile, certifications, and the gap live under /jobs and
        // /employees respectively - one fact bound to an existing aggregate, not a new top-level
        // collection.
        var root = app.MapGroup("");
        root.MapGetRequiredProfileEndpoint();
        root.MapSetRequiredOverrideEndpoint();
        root.MapRemoveRequiredOverrideEndpoint();
        root.MapGetHeldProfileEndpoint();
        root.MapRecordSelfDeclaredLevelEndpoint();
        root.MapRecordCertificationEndpoint();
        root.MapGetCompetencyGapEndpoint();
    }
}
