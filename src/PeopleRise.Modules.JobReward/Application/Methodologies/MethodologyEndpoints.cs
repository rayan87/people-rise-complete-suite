using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Application.Methodologies.AnswerOptions;
using PeopleRise.Modules.JobReward.Application.Methodologies.Factors;
using PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;
using PeopleRise.Modules.JobReward.Application.Methodologies.ImportExport;
using PeopleRise.Modules.JobReward.Application.Methodologies.Questions;
using PeopleRise.Modules.JobReward.Application.Methodologies.Versions;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

internal static class MethodologyEndpoints
{
    public static void MapMethodologyEndpoints(this IEndpointRouteBuilder app)
    {
        var methodologiesGroup = app.MapGroup("/methodologies");

        methodologiesGroup.MapListMethodologiesEndpoint();
        methodologiesGroup.MapCreateMethodologyEndpoint();
        methodologiesGroup.MapUpdateMethodologyEndpoint();
        methodologiesGroup.MapDeleteMethodologyEndpoint();
        methodologiesGroup.MapCreateVersionEndpoint();
        methodologiesGroup.MapImportMethodologyVersionEndpoint();

        var versions = app.MapGroup("/methodology-versions");

        //Versions
        versions.MapGetVersionDetailEndpoint();
        versions.MapPublishVersionEndpoint();
        versions.MapDeleteVersionEndpoint();
        versions.MapExportMethodologyVersionEndpoint();

        //Factors
        versions.MapAddFactorEndpoint();
        versions.MapUpdateFactorEndpoint();
        versions.MapDeleteFactorEndpoint();

        //Grade mappings
        versions.MapAddGradeMappingEndpoint();
        versions.MapUpdateGradeMappingEndpoint();
        versions.MapDeleteGradeMappingEndpoint();

        //Questions
        app.MapAddQuestionEndpoint();
        app.MapUpdateQuestionEndpoint();
        app.MapDeleteQuestionEndpoint();

        //Answer Options
        app.MapAddAnswerOptionEndpoint();
        app.MapUpdateAnswerOptionEndpoint();
        app.MapDeleteAnswerOptionEndpoint();
    }
}
