using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

internal static class MethodologyEndpoints
{
    public static void MapMethodologyEndpoints(this IEndpointRouteBuilder app)
    {
        var methodologies = app.MapGroup("/methodologies");

        methodologies.MapGet("/", async (ListMethodologiesHandler h, CancellationToken ct) =>
            (await h.Handle(new ListMethodologiesQuery(), ct)).ToHttp());

        methodologies.MapPost("/", async (CreateMethodologyCommand cmd, CreateMethodologyHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());

        methodologies.MapPut("/{id:guid}",
            async (Guid id, UpdateMethodologyRequest body, UpdateMethodologyHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateMethodologyCommand(id, body.NameEn, body.NameAr), ct)).ToHttp());

        methodologies.MapPost("/{id:guid}/versions",
            async (Guid id, CreateMethodologyVersionRequest body, CreateVersionHandler h, CancellationToken ct) =>
                (await h.Handle(new CreateVersionCommand(id, body.Note), ct)).ToHttp());

        var versions = app.MapGroup("/methodology-versions");

        versions.MapGet("/{id:guid}", async (Guid id, GetVersionDetailHandler h, CancellationToken ct) =>
            (await h.Handle(new GetVersionDetailQuery(id), ct)).ToHttp());

        versions.MapPost("/{id:guid}/publish", async (Guid id, PublishVersionHandler h, CancellationToken ct) =>
            (await h.Handle(new PublishVersionCommand(id), ct)).ToHttp());

        versions.MapPost("/{id:guid}/factors",
            async (Guid id, FactorRequest body, AddFactorHandler h, CancellationToken ct) =>
                (await h.Handle(new AddFactorCommand(id, body.Code, body.NameEn, body.NameAr, body.SortOrder, body.Weight), ct)).ToHttp());

        versions.MapPut("/{versionId:guid}/factors/{factorId:guid}",
            async (Guid versionId, Guid factorId, FactorRequest body, UpdateFactorHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateFactorCommand(versionId, factorId, body.Code, body.NameEn, body.NameAr, body.SortOrder, body.Weight), ct)).ToHttp());

        versions.MapDelete("/{versionId:guid}/factors/{factorId:guid}",
            async (Guid versionId, Guid factorId, DeleteFactorHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteFactorCommand(versionId, factorId), ct)).ToHttp());

        versions.MapPost("/{id:guid}/grade-mappings",
            async (Guid id, CreateGradeMappingRequest body, AddGradeMappingHandler h, CancellationToken ct) =>
                (await h.Handle(new AddGradeMappingCommand(id, body.GradeId, body.MinScore, body.MaxScore), ct)).ToHttp());

        versions.MapPut("/{versionId:guid}/grade-mappings/{mappingId:guid}",
            async (Guid versionId, Guid mappingId, UpdateGradeMappingRequest body, UpdateGradeMappingHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateGradeMappingCommand(versionId, mappingId, body.GradeId, body.MinScore, body.MaxScore), ct)).ToHttp());

        versions.MapDelete("/{versionId:guid}/grade-mappings/{mappingId:guid}",
            async (Guid versionId, Guid mappingId, DeleteGradeMappingHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteGradeMappingCommand(versionId, mappingId), ct)).ToHttp());

        app.MapPost("/factors/{factorId:guid}/questions",
            async (Guid factorId, QuestionRequest body, AddQuestionHandler h, CancellationToken ct) =>
                (await h.Handle(new AddQuestionCommand(factorId, body.QuestionTextEn, body.QuestionTextAr, body.HelpTextEn, body.HelpTextAr, body.SortOrder), ct)).ToHttp());

        app.MapPut("/factors/{factorId:guid}/questions/{questionId:guid}",
            async (Guid factorId, Guid questionId, QuestionRequest body, UpdateQuestionHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateQuestionCommand(factorId, questionId, body.QuestionTextEn, body.QuestionTextAr, body.HelpTextEn, body.HelpTextAr, body.SortOrder), ct)).ToHttp());

        app.MapDelete("/factors/{factorId:guid}/questions/{questionId:guid}",
            async (Guid factorId, Guid questionId, DeleteQuestionHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteQuestionCommand(factorId, questionId), ct)).ToHttp());

        app.MapPost("/questions/{questionId:guid}/options",
            async (Guid questionId, AnswerOptionRequest body, AddOptionHandler h, CancellationToken ct) =>
                (await h.Handle(new AddOptionCommand(questionId, body.LabelEn, body.LabelAr, body.Points, body.SortOrder), ct)).ToHttp());

        app.MapPut("/questions/{questionId:guid}/options/{optionId:guid}",
            async (Guid questionId, Guid optionId, AnswerOptionRequest body, UpdateOptionHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateOptionCommand(questionId, optionId, body.LabelEn, body.LabelAr, body.Points, body.SortOrder), ct)).ToHttp());

        app.MapDelete("/questions/{questionId:guid}/options/{optionId:guid}",
            async (Guid questionId, Guid optionId, DeleteOptionHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteOptionCommand(questionId, optionId), ct)).ToHttp());
    }
}
