using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Application.PayElementAmounts;

namespace PeopleRise.Core.Application.Employees;

internal static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/employees");

        group.MapListEmployeesEndpoint();
        group.MapGetEmployeeEndpoint();
        group.MapHireEmployeeEndpoint();
        group.MapMoveEmployeeEndpoint();
        group.MapSeparateEmployeeEndpoint();
        group.MapUpdateEmploymentStatusEndpoint();
        group.MapSetPayElementAmountEndpoint();
        group.MapListPayElementAmountsEndpoint();
    }
}
