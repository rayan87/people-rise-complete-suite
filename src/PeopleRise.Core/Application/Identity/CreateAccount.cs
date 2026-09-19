using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record CreateAccountCommand(string Email, string Password, Guid? EmployeeId);

// Core Spec §11: "an account is not an employee... not every account is an employee, because a
// consultant or an administrator may hold one without appearing anywhere on the payroll" - EmployeeId
// is optional, checked for existence only when the caller supplies one.
internal sealed class CreateAccountHandler(UserManager<Account> users, CoreDbContext db)
    : ICommandHandler<CreateAccountCommand, Result<AccountDto>>
{
    public async Task<Result<AccountDto>> Handle(CreateAccountCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Email)) return Error.Validation("Email is required.");

        if (cmd.EmployeeId is { } employeeId && !await db.Employees.AnyAsync(e => e.Id == employeeId, ct))
        {
            return Error.NotFound("Employee not found.");
        }

        var account = Account.Create(cmd.Email, cmd.EmployeeId);
        var result = await users.CreateAsync(account, cmd.Password);
        if (!result.Succeeded)
        {
            return Error.Validation(string.Join(' ', result.Errors.Select(e => e.Description)));
        }

        return account.ToDto();
    }
}

internal static class CreateAccountEndpoint
{
    public static void MapCreateAccountEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateAccountRequest body, CreateAccountHandler h, CancellationToken ct) =>
            (await h.Handle(new CreateAccountCommand(body.Email, body.Password, body.EmployeeId), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
