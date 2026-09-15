using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Application.Permissions;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// Core UI's direct-entry path for held profiles - provenance SelfDeclared, the lowest-precedence
// source (Core Spec §10/§11.2). Assessment and Performance each get their own writer with their own
// provenance once those products exist.
public sealed record RecordSelfDeclaredLevelCommand(Guid EmployeeId, Guid CompetencyId, int Level);

internal sealed class RecordSelfDeclaredLevelHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<RecordSelfDeclaredLevelCommand, Result<HeldProfileDto>>
{
    public async Task<Result<HeldProfileDto>> Handle(RecordSelfDeclaredLevelCommand cmd, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == cmd.EmployeeId, ct)) return Error.NotFound("Employee not found.");
        if (!await db.CompetencyDefinitions.AnyAsync(c => c.Id == cmd.CompetencyId, ct)) return Error.NotFound("Competency not found.");
        if (cmd.Level is < 1 or > 5) return Error.Validation("Level must be between 1 and 5.");

        db.HeldCompetencyProfiles.Add(HeldCompetencyProfile.Create(cmd.EmployeeId, cmd.CompetencyId, cmd.Level, HeldCompetencyProvenance.SelfDeclared));
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new HeldProfileChanged(cmd.EmployeeId), ct);
        await events.PublishAsync(new CompetencyGapChanged(cmd.EmployeeId), ct);

        return (await new GetHeldProfileHandler(db).Handle(new GetHeldProfileQuery(cmd.EmployeeId), ct));
    }
}

internal static class RecordSelfDeclaredLevelEndpoint
{
    public static void MapRecordSelfDeclaredLevelEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/employees/{employeeId:guid}/held-profile/self-declared",
            async (Guid employeeId, RecordSelfDeclaredLevelRequest body, RecordSelfDeclaredLevelHandler h, CancellationToken ct) =>
                (await h.Handle(new RecordSelfDeclaredLevelCommand(employeeId, body.CompetencyId, body.Level), ct)).ToHttp())
            .RequirePermission(Permission.ManageSensitiveData);
    }
}
