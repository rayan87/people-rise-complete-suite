using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Employees;

public sealed record ListEmployeesQuery();

internal sealed class ListEmployeesHandler(CoreDbContext db)
    : IQueryHandler<ListEmployeesQuery, Result<IReadOnlyList<EmployeeDto>>>
{
    public async Task<Result<IReadOnlyList<EmployeeDto>>> Handle(ListEmployeesQuery query, CancellationToken ct)
    {
        var rows = await EmployeeProjections.ListAsync(db, ct);
        return Result<IReadOnlyList<EmployeeDto>>.Success(rows);
    }
}

internal static class ListEmployeesEndpoint
{
    public static void MapListEmployeesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListEmployeesHandler h, CancellationToken ct) =>
            (await h.Handle(new ListEmployeesQuery(), ct)).ToHttp());
    }
}
