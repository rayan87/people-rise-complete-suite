using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.PayElements;

internal static class PayElementEndpoints
{
    public static void MapPayElementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pay-elements");

        group.MapListPayElementsEndpoint();
        group.MapCreatePayElementEndpoint();
        group.MapUpdatePayElementEndpoint();
        group.MapDeletePayElementEndpoint();
    }
}
