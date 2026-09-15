using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>An employee's held level for one competency, as recorded by one source (Core Spec §10).
/// Insert-only, like EvaluationAnswer - a later assessment from the same or a different source never
/// overwrites an earlier one, it adds a new row (Core Spec §3.1: provenance is never upgraded in
/// place). Several rows can coexist for the same employee+competency across different sources; which
/// one is "the" held level is resolved at read time by precedence, then recency within a source (see
/// HeldProfileProjections) - never decided here.</summary>
internal class HeldCompetencyProfile : ImmutableEntity
{
    public Guid EmployeeId { get; private set; }

    public Guid CompetencyId { get; private set; }

    public CompetencyDefinition? Competency { get; private set; }

    public int Level { get; private set; }   // 1-5

    public HeldCompetencyProvenance Source { get; private set; }

    private HeldCompetencyProfile() { }   // EF

    public static HeldCompetencyProfile Create(Guid employeeId, Guid competencyId, int level, HeldCompetencyProvenance source)
    {
        return new() { EmployeeId = employeeId, CompetencyId = competencyId, Level = level, Source = source };
    }
}

// Core Spec §3.1/§10. Precedence order (highest first) is a judgement call this codebase makes
// explicit since the spec only ranks three of the four by name: Assessed outranks a performance
// cycle, which outranks a self-declared level; CertificationDerived isn't placed in that sentence,
// but a certification is itself a verified, documentary fact, so it's ranked just under Assessed -
// see HeldProfileProjections.SourcePrecedence.
public enum HeldCompetencyProvenance { Assessed, PerformanceCycle, SelfDeclared, CertificationDerived }
