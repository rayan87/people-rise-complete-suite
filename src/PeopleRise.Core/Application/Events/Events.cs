using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Events;

// The core's published events (Core Spec §11.3). One-way: the core publishes and never subscribes
// to a product, so an uninstalled product simply has no subscriber - see SharedKernel.EventPublisher.
// Every event carries only identifiers + the changed fact, never a payload a subscriber could
// assemble by reading the core.
//
// Declared here for the full contract the spec names. Raised today: JobCreated, JobFamilyChanged,
// GradeCreated, JobGradeAssigned, BandPublished, OrgUnitClosed, PositionApproved/Occupied/Vacated,
// EmployeeHired/AssignmentChanged/Separated, PayElementAmountChanged. Declared but not yet raised:
// RequiredProfileChanged, HeldProfileChanged, CompetencyGapChanged - the competency spine (§10)
// doesn't exist yet.

// ---- Structure ----
public sealed record OrgUnitClosed(Guid OrgUnitId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record JobCreated(Guid JobId, string Code) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record JobFamilyChanged(Guid JobId, Guid? JobFamilyId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }

// ---- Ladder ----
public sealed record GradeCreated(Guid GradeId, string Code) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record JobGradeAssigned(Guid JobId, Guid GradeId, string Source) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }

// ---- Establishment ----
public sealed record PositionApproved(Guid PositionId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record PositionOccupied(Guid PositionId, Guid EmployeeId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record PositionVacated(Guid PositionId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }

// ---- Roster ----
public sealed record EmployeeHired(Guid EmployeeId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record EmployeeAssignmentChanged(Guid EmployeeId, Guid PositionId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record EmployeeSeparated(Guid EmployeeId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }

// ---- Pay ----
public sealed record PayElementAmountChanged(Guid EmployeeId, Guid PayElementId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record BandPublished(Guid GradeId, Guid BandId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }

// ---- Competency ----
public sealed record RequiredProfileChanged(Guid JobId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record HeldProfileChanged(Guid EmployeeId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
public sealed record CompetencyGapChanged(Guid EmployeeId) : IDomainEvent { public DateTime OccurredAt { get; } = DateTime.UtcNow; }
