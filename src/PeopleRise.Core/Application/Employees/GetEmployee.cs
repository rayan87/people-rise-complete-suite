using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Employees;

public sealed record GetEmployeeQuery(Guid Id);

internal sealed class GetEmployeeHandler(CoreDbContext db)
    : IQueryHandler<GetEmployeeQuery, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(GetEmployeeQuery query, CancellationToken ct)
    {
        var dto = await EmployeeProjections.ByIdAsync(db, query.Id, ct);
        return dto is null ? Error.NotFound("Employee not found.") : dto;
    }
}

internal static class GetEmployeeEndpoint
{
    public static void MapGetEmployeeEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, GetEmployeeHandler h, CancellationToken ct) =>
            (await h.Handle(new GetEmployeeQuery(id), ct)).ToHttp());
    }
}
