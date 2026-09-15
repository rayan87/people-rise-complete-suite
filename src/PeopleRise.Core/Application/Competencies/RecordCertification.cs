using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Application.Permissions;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// Certifications are live, not dormant (Core Spec §10) - recording one writes a held competency
// level with provenance CertificationDerived in the same transaction.
public sealed record RecordCertificationCommand(Guid EmployeeId, Guid CompetencyId, string NameEn, string? NameAr, int Level, DateOnly IssuedDate);

internal sealed class RecordCertificationHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<RecordCertificationCommand, Result<HeldProfileDto>>
{
    public async Task<Result<HeldProfileDto>> Handle(RecordCertificationCommand cmd, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == cmd.EmployeeId, ct)) return Error.NotFound("Employee not found.");
        if (!await db.CompetencyDefinitions.AnyAsync(c => c.Id == cmd.CompetencyId, ct)) return Error.NotFound("Competency not found.");
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");
        if (cmd.Level is < 1 or > 5) return Error.Validation("Level must be between 1 and 5.");

        db.Certifications.Add(Certification.Create(cmd.EmployeeId, cmd.CompetencyId, cmd.NameEn, cmd.NameAr, cmd.Level, cmd.IssuedDate));
        db.HeldCompetencyProfiles.Add(HeldCompetencyProfile.Create(cmd.EmployeeId, cmd.CompetencyId, cmd.Level, HeldCompetencyProvenance.CertificationDerived));
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new HeldProfileChanged(cmd.EmployeeId), ct);
        await events.PublishAsync(new CompetencyGapChanged(cmd.EmployeeId), ct);

        return (await new GetHeldProfileHandler(db).Handle(new GetHeldProfileQuery(cmd.EmployeeId), ct));
    }
}

internal static class RecordCertificationEndpoint
{
    public static void MapRecordCertificationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/employees/{employeeId:guid}/certifications",
            async (Guid employeeId, RecordCertificationRequest body, RecordCertificationHandler h, CancellationToken ct) =>
                (await h.Handle(new RecordCertificationCommand(employeeId, body.CompetencyId, body.NameEn, body.NameAr, body.Level, body.IssuedDate), ct)).ToHttp())
            .RequirePermission(Permission.ManageSensitiveData);
    }
}
