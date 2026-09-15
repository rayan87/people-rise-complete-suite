using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.SalaryBands;

// GenerateBands (the algorithmic, Compensation-specific action) is mapped separately by
// PeopleRise.Modules.JobReward under the same "/salary-bands" prefix - see JobRewardModule.
internal static class SalaryBandEndpoints
{
    public static void MapSalaryBandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/salary-bands");

        group.MapListSalaryBandsEndpoint();
        group.MapCreatSalaryBandEndpoint();
        group.MapUpdateSalaryBandEndpoint();
    }
}
