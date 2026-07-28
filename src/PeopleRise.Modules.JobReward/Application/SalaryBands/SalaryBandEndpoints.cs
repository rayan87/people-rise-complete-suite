using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

internal static class SalaryBandEndpoints
{
    public static void MapSalaryBandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/salary-bands");
        
        group.MapListSalaryBandsEndpoint();
        group.MapCreatSalaryBandEndpoint();
        group.MapUpdateSalaryBandEndpoint();
        group.MapGenerateBandsEndpoint();
    }
}


