using PeopleRise.Core.Domain;

namespace PeopleRise.Core.Application.Competencies;

internal static class HeldProfileProjections
{
    // Precedence order (highest first) - a judgement call, documented on HeldCompetencyProfile:
    // Assessed > CertificationDerived > PerformanceCycle > SelfDeclared. Lower number = higher
    // precedence, so this sorts directly with OrderBy.
    public static int SourcePrecedence(HeldCompetencyProvenance source) => source switch
    {
        HeldCompetencyProvenance.Assessed => 0,
        HeldCompetencyProvenance.CertificationDerived => 1,
        HeldCompetencyProvenance.PerformanceCycle => 2,
        HeldCompetencyProvenance.SelfDeclared => 3,
        _ => int.MaxValue,
    };
}
