using Microsoft.AspNetCore.Http;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application;

/// <summary>Maps a SharedKernel <see cref="Result"/> onto an HTTP response. This is the one place
/// the error categories become status codes; SharedKernel stays web-agnostic.</summary>
internal static class HttpResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();

    public static IResult ToHttp(this Result result) =>
        result.IsSuccess ? Results.Ok() : result.Error!.ToProblem();

    private static IResult ToProblem(this Error error) =>
        Results.Problem(detail: error.Message, statusCode: error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        });
}
